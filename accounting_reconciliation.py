"""
ShreeSeva Medical Accounting Reconciliation Script
Takeover Date: 29-May-2025

Bank Statement matching:
  1. CHQ rows -> PAIDMAST by last digits of narration
  2. GPAY/NEFT/UPI -> PAIDMAST by exact withdrawal amount (unique match only)
  3. Unmatched CHQ -> flag as NOT_IN_SYSTEM (pre-takeover, not in new software)

System_Purchase columns filled:
  Date           = PAIDMAST.CHQDATE
  Type           = CHQ / GPAY / NEFT etc.
  ChQ No.        = PAIDMAST.CHQNO
  Previous Pend  = sum PURPAIDAMT where BILLDATE < 29-May-2025 (same PADVCHNO)
  Old Account    = PURPAIDAMT of the specific matched PAIDTRAN row
  Pending        = PURBALAMT if partially paid; full Amount if no payment found
  Cash           = Amount if payment type is CASH
"""

import struct, re, openpyxl
from openpyxl.styles import PatternFill, Font
from datetime import date
from collections import defaultdict

TAKEOVER_DT = date(2025, 5, 29)

# ── Colors ────────────────────────────────────────────────────────────────────
C_HDR_BLUE   = "1F4E79"  # dark blue header
C_HDR_ORG    = "FF6600"  # orange new-col header
C_GREEN      = "E2EFDA"  # matched CHQ
C_YELLOW     = "FFF2CC"  # date/type/chq fill
C_GOLD       = "FFD966"  # prev pending
C_BLUE_LIGHT = "BDD7EE"  # old account
C_RED_LIGHT  = "FCE4D6"  # not-in-system / unpaid pending
C_PURPLE     = "E2CFEF"  # gpay/neft match
C_GREY       = "D9D9D9"  # partial pending

def fill(color): return PatternFill(start_color=color, end_color=color, fill_type="solid")
def font(color="000000", bold=False): return Font(color=color, bold=bold)


# ── DBF Reader ────────────────────────────────────────────────────────────────
def read_dbf(filename):
    with open(filename, "rb") as f:
        h = f.read(32)
        nr = struct.unpack("<I", h[4:8])[0]
        hs = struct.unpack("<H", h[8:10])[0]
        rs = struct.unpack("<H", h[10:12])[0]
        flds = []
        while True:
            fd = f.read(32)
            if fd[0] == 0x0D: break
            flds.append((fd[:11].replace(b"\x00", b"").decode("latin-1"), chr(fd[11]), fd[16]))
        f.seek(hs)
        recs = []
        for _ in range(nr):
            rec = f.read(rs)
            if not rec or rec[0] == 0x1A: break
            if rec[0] == ord("*"): continue
            row = {}; pos = 1
            for n, t, l in flds:
                row[n] = rec[pos:pos+l].decode("latin-1").strip(); pos += l
            recs.append(row)
    return recs

def parse_date(s):
    if s and len(s) == 8 and s.isdigit():
        try: return date(int(s[:4]), int(s[4:6]), int(s[6:8]))
        except: pass
    return None

def flt(v, default=0.0):
    try: return float(v) if v else default
    except: return default

def safe_int_str(v):
    try: return str(int(float(v)))
    except: return None


# ── Load DBF ──────────────────────────────────────────────────────────────────
print("=" * 65)
print("Loading DBF tables...")
account  = read_dbf("DataBase/ACCOUNT.DBF")
paidmast = read_dbf("DataBase/PAIDMAST.DBF")
paidtran = read_dbf("DataBase/PAIDTRAN.DBF")
print(f"  ACCOUNT:{len(account)}  PAIDMAST:{len(paidmast)}  PAIDTRAN:{len(paidtran)}")

acct_by_id    = {r["ACCOID"]: r for r in account}
mast_by_vch   = {r["PADVCHNO"]: r for r in paidmast}
tran_by_padvch = defaultdict(list)
tran_by_purvch = defaultdict(list)
for r in paidtran:
    tran_by_padvch[r["PADVCHNO"]].append(r)
    tran_by_purvch[r["PURVCHNO"]].append(r)

# CHQ (integer) -> PAIDMAST row
chq_lookup = {}
for r in paidmast:
    chq = r["CHQNO"].strip()
    if chq.isdigit():
        chq_lookup[int(chq)] = r

# GPAY/NEFT/UPI: amount (int) -> list of PAIDMAST rows  (for unique-amount matching)
gpay_neft_by_amt = defaultdict(list)
for r in paidmast:
    chq = r["CHQNO"].strip().upper()
    if not chq.isdigit():   # non-CHQ payment (GPAY / NEFT / RTGS etc.)
        try:
            amt = int(float(r["PADVCHAMT"]))
            if amt > 0:
                gpay_neft_by_amt[amt].append(r)
        except: pass

# Pre-calculate Previous Pending per PADVCHNO (sum PURPAIDAMT for pre-takeover bills)
prev_pend_by_padvch = defaultdict(float)
for r in paidtran:
    bd = parse_date(r["BILLDATE"])
    if bd and bd < TAKEOVER_DT:
        prev_pend_by_padvch[r["PADVCHNO"]] += flt(r["PURPAIDAMT"])


# ── Export joined DBF ─────────────────────────────────────────────────────────
print("\nExporting DBF_Joined_Data.xlsx ...")
wb_dbf = openpyxl.Workbook()

ws1 = wb_dbf.active
ws1.title = "PAIDMAST_with_Party"
ws1.append(["VoucherNo","VoucherDate","PartyID","PartyName","Amount",
            "Discount","Pending","CHQNo","CHQDate","PayType","Narration","FYear"])
for r in paidmast:
    acct = acct_by_id.get(r["ACCOID"], {})
    ws1.append([
        int(r["PADVCHNO"]) if r["PADVCHNO"].isdigit() else r["PADVCHNO"],
        parse_date(r["PADVCHDATE"]),
        int(r["ACCOID"]) if r["ACCOID"].isdigit() else r["ACCOID"],
        acct.get("ACCONM",""),
        flt(r["PADVCHAMT"]), flt(r["PADDISC"]), flt(r["PENDING"]),
        r["CHQNO"], parse_date(r["CHQDATE"]), r["PADTRDTYPE"], r["NARA"], r["FINYEAR"],
    ])

ws2 = wb_dbf.create_sheet("PAIDTRAN_Full")
ws2.append(["PayVoucherNo","PayDate","PartyID","PartyName","BillNo","BillDate",
            "BillAmt","PaidAmt","BalAmt","Discount","PurchVoucherNo","CHQNo",
            "CHQDate","PayType","PriorToTakeover"])
for r in paidtran:
    mast = mast_by_vch.get(r["PADVCHNO"], {})
    acct = acct_by_id.get(r["ACCOID"], {})
    bd   = parse_date(r["BILLDATE"])
    ws2.append([
        int(r["PADVCHNO"]) if r["PADVCHNO"].isdigit() else r["PADVCHNO"],
        parse_date(r["PADVCHDATE"]),
        int(r["ACCOID"]) if r["ACCOID"].isdigit() else r["ACCOID"],
        acct.get("ACCONM",""),
        r["BILLNO"], bd,
        flt(r["PURNETAMT"]), flt(r["PURPAIDAMT"]), flt(r["PURBALAMT"]), flt(r["DISCOUNT"]),
        int(r["PURVCHNO"]) if r["PURVCHNO"].isdigit() else r["PURVCHNO"],
        mast.get("CHQNO",""), parse_date(mast.get("CHQDATE","")), r["PADTRDTYPE"],
        "YES" if (bd and bd < TAKEOVER_DT) else "NO",
    ])

hf = fill(C_HDR_BLUE); hfont = font("FFFFFF", bold=True)
for ws in [ws1, ws2]:
    for cell in ws[1]: cell.fill = hf; cell.font = hfont
    ws.freeze_panes = ws["A2"]
wb_dbf.save("DBF_Joined_Data.xlsx")
print("  Saved: DBF_Joined_Data.xlsx")


# ── Update Bank Statement ─────────────────────────────────────────────────────
print("\nUpdating Old_Bank_Statement_Shree_Seva_Medical.xlsx ...")

wb_bank = openpyxl.load_workbook("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
ws_bank = wb_bank.active
bank_headers = [c.value for c in ws_bank[1]]

def get_or_add(ws, headers, name):
    if name in headers:
        return headers.index(name) + 1
    col = ws.max_column + 1
    c = ws.cell(row=1, column=col, value=name)
    c.fill = fill(C_HDR_ORG); c.font = font("FFFFFF", bold=True)
    return col

# Refresh headers after any previous run
bank_headers = [c.value for c in ws_bank[1]]
vch_col  = get_or_add(ws_bank, bank_headers, "Voucher_No")
pty_col  = get_or_add(ws_bank, bank_headers, "Party_Name")
note_col = get_or_add(ws_bank, bank_headers, "Match_Status")

# Refresh header list
bank_headers = [c.value for c in ws_bank[1]]

cnt_chq = cnt_gpay = cnt_not_sys = 0

for row_idx in range(2, ws_bank.max_row + 1):
    narr  = str(ws_bank.cell(row=row_idx, column=2).value or "")
    wd_val = ws_bank.cell(row=row_idx, column=6).value  # Withdrawal_Amt
    txtype = str(ws_bank.cell(row=row_idx, column=5).value or "")

    mast_row = None
    match_type = ""

    # ── Method 1: CHQ number match (last digits of narration)
    nums = re.findall(r"\d+", narr)
    if nums:
        mast_row = chq_lookup.get(int(nums[-1]))
        if mast_row:
            match_type = "CHQ_MATCHED"

    # ── Method 2: GPAY/NEFT/UPI – match by exact withdrawal amount
    if not mast_row and wd_val and txtype == "DR":
        try:
            amt = int(float(wd_val))
            candidates = gpay_neft_by_amt.get(amt, [])
            if len(candidates) == 1:
                mast_row = candidates[0]
                match_type = "GPAY_AMT_MATCHED"
        except: pass

    # ── Method 3: CHQ in narration but NOT in PAIDMAST (pre-takeover / unrecorded)
    if not mast_row and ("CHQ PAID" in narr.upper() or "CHQ" in narr.upper()):
        if nums:
            match_type = "NOT_IN_SYSTEM"

    if mast_row:
        # Party Name
        acct = acct_by_id.get(mast_row["ACCOID"], {})
        party = acct.get("ACCONM", "")
        ws_bank.cell(row=row_idx, column=pty_col, value=party)

        # PURVCHNO comma-separated
        tran_rows = tran_by_padvch.get(mast_row["PADVCHNO"], [])
        purvchno_csv = ",".join(r["PURVCHNO"] for r in tran_rows if r["PURVCHNO"])
        ws_bank.cell(row=row_idx, column=vch_col, value=purvchno_csv)

        # Color
        clr = C_GREEN if match_type == "CHQ_MATCHED" else C_PURPLE
        ws_bank.cell(row=row_idx, column=vch_col).fill  = fill(clr)
        ws_bank.cell(row=row_idx, column=pty_col).fill  = fill(clr)

        if match_type == "CHQ_MATCHED": cnt_chq += 1
        else: cnt_gpay += 1

    if match_type == "NOT_IN_SYSTEM":
        ws_bank.cell(row=row_idx, column=note_col, value="NOT_IN_SYSTEM")
        ws_bank.cell(row=row_idx, column=note_col).fill = fill(C_RED_LIGHT)
        cnt_not_sys += 1
    elif mast_row:
        ws_bank.cell(row=row_idx, column=note_col, value=match_type)
        ws_bank.cell(row=row_idx, column=note_col).fill = fill(C_GREEN if match_type=="CHQ_MATCHED" else C_PURPLE)

wb_bank.save("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
print(f"  CHQ matched    : {cnt_chq}")
print(f"  GPAY/NEFT matched (by amount): {cnt_gpay}")
print(f"  NOT_IN_SYSTEM  : {cnt_not_sys}  (CHQ not recorded in new software)")
print("  Saved.")


# ── Update System_Purchase ────────────────────────────────────────────────────
print("\nUpdating System_Puchase.xlsx ...")

wb_sys = openpyxl.load_workbook("System_Puchase.xlsx")
ws_sys = wb_sys.active
sys_headers = [c.value for c in ws_sys[1]]

def colp(name):
    for i, h in enumerate(sys_headers):
        if h and str(h).strip().lower() == name.lower():
            return i + 1
    return None

date_col     = colp("Date")
type_col     = colp("Type")
chqno_col    = colp("ChQ No.")
prevpend_col = colp("Previous Pending")
oldacct_col  = colp("Old Account")
newacct_col  = colp("New Account")
pending_col  = colp("Pending")
cash_col     = colp("Cash")
amount_col   = colp("Amount")

print(f"  Cols → Date:{date_col} Type:{type_col} CHQ:{chqno_col} "
      f"PrevPend:{prevpend_col} OldAcct:{oldacct_col} "
      f"Pend:{pending_col} Cash:{cash_col}")

cnt_matched = cnt_prev = cnt_pending_unpaid = cnt_pending_partial = cnt_cash = 0

for row_idx in range(2, ws_sys.max_row + 1):
    vou_cell = ws_sys.cell(row=row_idx, column=1).value
    vou_str  = safe_int_str(vou_cell)
    if not vou_str:
        continue

    amount = flt(ws_sys.cell(row=row_idx, column=amount_col).value) if amount_col else 0

    # Find PAIDTRAN rows for this purchase voucher
    tran_rows = tran_by_purvch.get(vou_str, [])

    if not tran_rows:
        # ── UNPAID BILL: Pending = full Amount ──────────────────────────────
        if pending_col and amount:
            c = ws_sys.cell(row=row_idx, column=pending_col, value=amount)
            c.fill = fill(C_RED_LIGHT)
            cnt_pending_unpaid += 1
        continue

    cnt_matched += 1

    # Use first PAIDTRAN row to get PAIDMAST
    tr       = tran_rows[0]
    padvchno = tr["PADVCHNO"]
    mast_row = mast_by_vch.get(padvchno, {})

    if mast_row:
        chq_no = mast_row.get("CHQNO", "").strip()
        chq_dt = parse_date(mast_row.get("CHQDATE", ""))

        if chq_no.upper() in ("GPAY","NEFT","RTGS","UPI","CASH"):
            display_type = chq_no.upper()
            chq_display  = ""
        else:
            display_type = "CHQ"
            chq_display  = chq_no

        # Date
        if date_col:
            ws_sys.cell(row=row_idx, column=date_col, value=chq_dt).fill = fill(C_YELLOW)
        # Type
        if type_col:
            ws_sys.cell(row=row_idx, column=type_col, value=display_type).fill = fill(C_YELLOW)
        # CHQ No.
        if chqno_col:
            ws_sys.cell(row=row_idx, column=chqno_col, value=chq_display).fill = fill(C_YELLOW)

        # Cash column
        if chq_no.upper() == "CASH" and cash_col:
            paid_amt = flt(tr["PURPAIDAMT"])
            ws_sys.cell(row=row_idx, column=cash_col, value=paid_amt).fill = fill(C_GREY)
            cnt_cash += 1

    # Previous Pending (sum of pre-takeover PURPAIDAMT in same payment)
    prev_amt = prev_pend_by_padvch.get(padvchno, 0)
    if prev_amt > 0 and prevpend_col:
        ws_sys.cell(row=row_idx, column=prevpend_col, value=round(prev_amt, 2)).fill = fill(C_GOLD)
        cnt_prev += 1

    # Old Account = PURPAIDAMT of this specific matched row
    paid_amt = flt(tr["PURPAIDAMT"])
    if paid_amt and oldacct_col:
        ws_sys.cell(row=row_idx, column=oldacct_col, value=round(paid_amt, 2)).fill = fill(C_BLUE_LIGHT)

    # Pending = PURBALAMT (remaining unpaid portion)
    bal_amt = flt(tr["PURBALAMT"])
    if bal_amt > 0 and pending_col:
        ws_sys.cell(row=row_idx, column=pending_col, value=round(bal_amt, 2)).fill = fill(C_GREY)
        cnt_pending_partial += 1

wb_sys.save("System_Puchase.xlsx")
print(f"  Matched (paid)         : {cnt_matched}")
print(f"  With Previous Pending  : {cnt_prev}")
print(f"  Unpaid (Pending=Amount): {cnt_pending_unpaid}")
print(f"  Partial pay (Pending>0): {cnt_pending_partial}")
print(f"  Cash payments          : {cnt_cash}")
print("  Saved.")


# ── Final Summary ─────────────────────────────────────────────────────────────
print("\n" + "=" * 65)
print("FINAL RECONCILIATION SUMMARY")
print("=" * 65)
print(f"  Takeover Date   : 29-May-2025")
print()
print("  BANK STATEMENT:")
print(f"    CHQ matched (in system)    : {cnt_chq}")
print(f"    GPAY/NEFT matched (by amt) : {cnt_gpay}")
print(f"    NOT_IN_SYSTEM (pre-takeover CHQ) : {cnt_not_sys}")
print()
print("  SYSTEM_PURCHASE:")
print(f"    Bills with payment found   : {cnt_matched}")
print(f"    Bills UNPAID (Pending set) : {cnt_pending_unpaid}")
print(f"    Bills partial pay          : {cnt_pending_partial}")
total_prev = sum(prev_pend_by_padvch.values())
print(f"    Total Previous Pending     : Rs. {total_prev:,.2f}")
print()
print("  OUTPUT FILES:")
print("    DBF_Joined_Data.xlsx")
print("    Old_Bank_Statement_Shree_Seva_Medical.xlsx")
print("    System_Puchase.xlsx")
print("=" * 65)
