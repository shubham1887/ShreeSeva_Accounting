"""
ShreeSeva Medical - Accounting Reconciliation
Takeover Date: 29-May-2025

Logic:
  1. Join ACCOUNT + PAIDMAST + PAIDTRAN into one sheet (DBF_Joined_Data.xlsx)
  2. For each PAIDMAST CHQNO, check if it appears in Bank Statement (CHQ PAID narration)
  3. System_Purchase (per Vou.No. = PAIDTRAN.PURVCHNO, BILLDATE >= 29-May):
       - CHQ found in bank   -> Date, ChQ No., Old Account = PURPAIDAMT
       - CHQ NOT in bank     -> Pending = System_Purchase Amount
       - No payment at all   -> Pending = System_Purchase Amount
     Old Account = ONLY bank-confirmed entries. Nothing else.
"""

import struct, re, openpyxl
from openpyxl.styles import PatternFill, Font
from datetime import date
from collections import defaultdict

TAKEOVER = date(2025, 5, 29)

# ── Styles ────────────────────────────────────────────────────────────────────
def F(c): return PatternFill(start_color=c, end_color=c, fill_type="solid")
def FT(c="000000", bold=False): return Font(color=c, bold=bold)

BLUE   = "1F4E79"; WHITE  = "FFFFFF"
GREEN  = "E2EFDA"; GOLD   = "FFD966"
YELLOW = "FFF2CC"; LBLUE  = "BDD7EE"
RED    = "FCE4D6"; GREY   = "D9D9D9"
ORANGE = "FF6600"

# ── DBF Reader ────────────────────────────────────────────────────────────────
def read_dbf(path):
    with open(path, "rb") as f:
        h  = f.read(32)
        nr = struct.unpack("<I", h[4:8])[0]
        hs = struct.unpack("<H", h[8:10])[0]
        rs = struct.unpack("<H", h[10:12])[0]
        fields = []
        while True:
            fd = f.read(32)
            if fd[0] == 0x0D: break
            fields.append((fd[:11].replace(b"\x00",b"").decode("latin-1"), chr(fd[11]), fd[16]))
        f.seek(hs)
        rows = []
        for _ in range(nr):
            rec = f.read(rs)
            if not rec or rec[0] == 0x1A: break
            if rec[0] == ord("*"): continue
            row = {}; pos = 1
            for n, t, l in fields:
                row[n] = rec[pos:pos+l].decode("latin-1").strip(); pos += l
            rows.append(row)
    return rows

def to_date(s):
    if s and len(s) == 8 and s.isdigit():
        try: return date(int(s[:4]), int(s[4:6]), int(s[6:8]))
        except: pass
    return None

def flt(v):
    try: return float(v) if v else 0.0
    except: return 0.0

# ── Load DBF ──────────────────────────────────────────────────────────────────
print("=" * 60)
print("Step 1 : Loading DBF files...")
account  = read_dbf("DataBase/ACCOUNT.DBF")
paidmast = read_dbf("DataBase/PAIDMAST.DBF")
paidtran = read_dbf("DataBase/PAIDTRAN.DBF")
print(f"  ACCOUNT:{len(account)}  PAIDMAST:{len(paidmast)}  PAIDTRAN:{len(paidtran)}")

# Lookup maps
acct_map  = {r["ACCOID"]: r["ACCONM"] for r in account}
mast_map  = {r["PADVCHNO"]: r for r in paidmast}

# PAIDTRAN grouped by PURVCHNO (only post-takeover bills)
tran_by_purvch = defaultdict(list)
for r in paidtran:
    bd = to_date(r["BILLDATE"])
    if bd and bd >= TAKEOVER:
        tran_by_purvch[r["PURVCHNO"]].append(r)

# PAIDTRAN grouped by PADVCHNO (for bank statement Voucher_No column)
tran_by_padvch = defaultdict(list)
for r in paidtran:
    tran_by_padvch[r["PADVCHNO"]].append(r)

# ── Step 1: Build Joined DBF Excel ────────────────────────────────────────────
print("\nStep 1b: Exporting joined DBF -> DBF_Joined_Data.xlsx ...")
wb_j = openpyxl.Workbook()

# Sheet 1: PAIDMAST joined with ACCOUNT
ws_j1 = wb_j.active; ws_j1.title = "PAIDMAST_ACCOUNT"
ws_j1.append(["VchNo","VchDate","PartyID","PartyName","CHQNo","CHQDate",
               "Amount","Discount","Pending","PayType","Narration","FYear"])
for r in paidmast:
    ws_j1.append([
        int(r["PADVCHNO"]) if r["PADVCHNO"].isdigit() else r["PADVCHNO"],
        to_date(r["PADVCHDATE"]),
        int(r["ACCOID"]) if r["ACCOID"].isdigit() else r["ACCOID"],
        acct_map.get(r["ACCOID"], ""),
        r["CHQNO"], to_date(r["CHQDATE"]),
        flt(r["PADVCHAMT"]), flt(r["PADDISC"]), flt(r["PENDING"]),
        r["PADTRDTYPE"], r["NARA"], r["FINYEAR"],
    ])

# Sheet 2: PAIDTRAN joined with PAIDMAST + ACCOUNT
ws_j2 = wb_j.create_sheet("PAIDTRAN_FULL")
ws_j2.append(["PayVchNo","PayDate","PartyID","PartyName","BillNo","BillDate",
               "NetAmt","PaidAmt","BalAmt","Discount","PurchVchNo","CHQNo",
               "CHQDate","PayType","PostTakeover"])
for r in paidtran:
    m  = mast_map.get(r["PADVCHNO"], {})
    bd = to_date(r["BILLDATE"])
    ws_j2.append([
        int(r["PADVCHNO"])  if r["PADVCHNO"].isdigit()  else r["PADVCHNO"],
        to_date(r["PADVCHDATE"]),
        int(r["ACCOID"])    if r["ACCOID"].isdigit()     else r["ACCOID"],
        acct_map.get(r["ACCOID"], ""),
        r["BILLNO"], bd,
        flt(r["PURNETAMT"]), flt(r["PURPAIDAMT"]), flt(r["PURBALAMT"]),
        flt(r["DISCOUNT"]),
        int(r["PURVCHNO"]) if r["PURVCHNO"].isdigit() else r["PURVCHNO"],
        m.get("CHQNO",""), to_date(m.get("CHQDATE","")), r["PADTRDTYPE"],
        "YES" if (bd and bd >= TAKEOVER) else "NO",
    ])

hf = F(BLUE); hft = FT(WHITE, bold=True)
for ws in [ws_j1, ws_j2]:
    for c in ws[1]: c.fill = hf; c.font = hft
    ws.freeze_panes = ws["A2"]
wb_j.save("DBF_Joined_Data.xlsx")
print("  Saved: DBF_Joined_Data.xlsx")


# ── Step 2: Build Bank Statement CHQ + GPAY lookup ───────────────────────────
print("\nStep 2 : Scanning Bank Statement for CHQ PAID + GPAY/NEFT entries...")
wb_bank = openpyxl.load_workbook("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
ws_bank = wb_bank.active

# CHQ: chq_int -> bank date
bank_chq_date = {}
# GPAY/NEFT: withdrawal_amount -> paidmast_row (only if unique match)
gpay_amt_to_mast = {}

# Build GPAY/NEFT lookup from PAIDMAST (amount -> list of rows)
gpay_by_amt = defaultdict(list)
for r in paidmast:
    chq = r["CHQNO"].strip().upper()
    if chq in ("GPAY", "UPI", "NEFT", "RTGS", "IMPS", "CASH"):
        try:
            amt = int(float(r["PADVCHAMT"]))
            if amt > 0:
                gpay_by_amt[amt].append(r)
        except: pass

# Scan bank statement
for row in ws_bank.iter_rows(min_row=2, values_only=True):
    narr = str(row[1]) if row[1] else ""
    wd   = row[5]   # Withdrawal_Amt (DR entries)

    # ── CHQ PAID entries
    if "CHQ PAID" in narr.upper():
        nums = re.findall(r"\d+", narr)
        if nums:
            chq_int = int(nums[-1])
            if chq_int not in bank_chq_date:
                bank_dt = row[0]
                if hasattr(bank_dt, "date"): bank_dt = bank_dt.date()
                bank_chq_date[chq_int] = bank_dt

    # ── GPAY / UPI / NEFT / RTGS entries (match by withdrawal amount)
    elif wd and any(x in narr.upper() for x in ["UPI","GPAY","PAYTM","PHONEPE","NEFT","RTGS","IMPS"]):
        try:
            amt = int(float(wd))
            candidates = gpay_by_amt.get(amt, [])
            if len(candidates) == 1:          # unique amount = confident match
                gpay_amt_to_mast[amt] = candidates[0]
        except: pass

# Set of confirmed CHQs
confirmed_chqs = set(bank_chq_date.keys())
# Set of confirmed GPAY PADVCHNO
confirmed_gpay_padvch = {r["PADVCHNO"] for r in gpay_amt_to_mast.values()}

print(f"  CHQ PAID confirmed   : {len(confirmed_chqs)}")
print(f"  GPAY/NEFT confirmed  : {len(gpay_amt_to_mast)} payments -> "
      f"{sum(len(tran_by_padvch.get(r['PADVCHNO'],[])) for r in gpay_amt_to_mast.values())} PAIDTRAN entries")

# PAIDMAST CHQ -> PADVCHNO map (numeric CHQs, list for duplicates)
chq_to_padvch = defaultdict(list)
for r in paidmast:
    c = r["CHQNO"].strip()
    if c.isdigit():
        chq_to_padvch[int(c)].append(r["PADVCHNO"])


# ── Step 3: Update Bank Statement columns ─────────────────────────────────────
# Add: Voucher_No (PURVCHNO list), Party_Name, Match_Status
bh = [c.value for c in ws_bank[1]]

def get_or_add_col(ws, headers, name):
    if name in headers:
        return headers.index(name) + 1
    col = ws.max_column + 1
    c = ws.cell(row=1, column=col, value=name)
    c.fill = F(ORANGE); c.font = FT(WHITE, bold=True)
    return col

bvch_col  = get_or_add_col(ws_bank, bh, "Voucher_No")
bpty_col  = get_or_add_col(ws_bank, bh, "Party_Name")
bsts_col  = get_or_add_col(ws_bank, bh, "Match_Status")

cnt_bank_chq = cnt_bank_not_sys = 0
for row_idx in range(2, ws_bank.max_row + 1):
    narr = str(ws_bank.cell(row=row_idx, column=2).value or "")
    if "CHQ PAID" not in narr.upper():
        continue
    nums = re.findall(r"\d+", narr)
    if not nums:
        continue
    chq_int = int(nums[-1])

    padvchno_list = chq_to_padvch.get(chq_int, [])
    if padvchno_list:
        # Confirmed in both bank and PAIDMAST
        # Write PURVCHNO list from PAIDTRAN
        purvchno_all = []
        party_names  = set()
        for pv in padvchno_list:
            for tr in tran_by_padvch.get(pv, []):
                purvchno_all.append(tr["PURVCHNO"])
            mast = mast_map.get(pv, {})
            party_names.add(acct_map.get(mast.get("ACCOID",""), ""))

        ws_bank.cell(row=row_idx, column=bvch_col, value=",".join(purvchno_all)).fill = F(GREEN)
        ws_bank.cell(row=row_idx, column=bpty_col, value=" / ".join(p for p in party_names if p)).fill = F(GREEN)
        ws_bank.cell(row=row_idx, column=bsts_col, value="CHQ_MATCHED").fill = F(GREEN)
        cnt_bank_chq += 1
    else:
        # CHQ in bank but not in PAIDMAST -> pre-system
        ws_bank.cell(row=row_idx, column=bsts_col, value="NOT_IN_SYSTEM").fill = F(RED)
        cnt_bank_not_sys += 1

wb_bank.save("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
print(f"  CHQ matched (in system)  : {cnt_bank_chq}")
print(f"  NOT_IN_SYSTEM (pre-takeover) : {cnt_bank_not_sys}")
print("  Saved: Old_Bank_Statement_Shree_Seva_Medical.xlsx")


# ── Step 4: Update System_Purchase ───────────────────────────────────────────
print("\nStep 4 : Updating System_Puchase.xlsx ...")
wb_sys = openpyxl.load_workbook("System_Puchase.xlsx")
ws_sys = wb_sys.active
sh = [c.value for c in ws_sys[1]]

def col(name):
    for i, h in enumerate(sh):
        if h and str(h).strip().lower() == name.lower():
            return i + 1
    return None

amt_col      = col("Amount")
date_col     = col("Date")
type_col     = col("Type")
chqno_col    = col("ChQ No.")
prev_col     = col("Previous Pending")
old_col      = col("Old Account")
new_col      = col("New Account")
pend_col     = col("Pending")
cash_col     = col("Cash")

# Previous Pending per PADVCHNO = sum PURPAIDAMT for BILLDATE < TAKEOVER
prev_by_padvch = defaultdict(float)
for r in paidtran:
    bd = to_date(r["BILLDATE"])
    if bd and bd < TAKEOVER:
        prev_by_padvch[r["PADVCHNO"]] += flt(r["PURPAIDAMT"])

cnt_old = cnt_gpay = cnt_pend = cnt_prev = 0

# Columns to clear before fresh fill
cols_to_clear = [c for c in [date_col, type_col, chqno_col, prev_col, old_col, new_col, pend_col, cash_col] if c]

for ri in range(2, ws_sys.max_row + 1):
    vou_val = ws_sys.cell(row=ri, column=1).value
    if vou_val is None:
        continue
    try:
        vou_str = str(int(float(vou_val)))
    except:
        continue

    # Clear old values first (fresh run)
    for c in cols_to_clear:
        ws_sys.cell(row=ri, column=c).value = None
        ws_sys.cell(row=ri, column=c).fill  = F("FFFFFF")

    # System_Purchase Amount (invoice total)
    inv_amt = flt(ws_sys.cell(row=ri, column=amt_col).value) if amt_col else 0

    # Find PAIDTRAN entries (post-takeover only)
    tran_rows = tran_by_purvch.get(vou_str, [])

    if not tran_rows:
        # No payment recorded -> leave blank
        continue

    tr       = tran_rows[0]
    padvchno = tr["PADVCHNO"]
    mast     = mast_map.get(padvchno, {})
    chq_no   = mast.get("CHQNO", "").strip()
    chq_dt   = to_date(mast.get("CHQDATE", ""))
    paid_amt = flt(tr["PURPAIDAMT"])

    # Payment type
    chq_upper = chq_no.upper()
    if chq_upper in ("GPAY", "UPI", "NEFT", "RTGS", "IMPS", "CASH"):
        ptype       = chq_upper
        chq_display = ""
    else:
        ptype       = "CHQ"
        chq_display = chq_no

    # Is this payment confirmed by bank?
    chq_in_bank  = chq_no.isdigit() and int(chq_no) in confirmed_chqs
    gpay_in_bank = padvchno in confirmed_gpay_padvch

    bank_confirmed = chq_in_bank or gpay_in_bank

    # Previous Pending (old bills bundled in same payment voucher)
    prev_amt = prev_by_padvch.get(padvchno, 0)
    if prev_amt > 0 and prev_col:
        ws_sys.cell(row=ri, column=prev_col, value=round(prev_amt, 2)).fill = F(GOLD)
        cnt_prev += 1

    if bank_confirmed:
        # ── CONFIRMED by bank -> fill Date, Type, CHQ/GPAY No., Old Account
        if date_col:
            ws_sys.cell(row=ri, column=date_col,  value=chq_dt).fill    = F(YELLOW)
        if type_col:
            ws_sys.cell(row=ri, column=type_col,  value=ptype).fill     = F(YELLOW)
        if chqno_col:
            ws_sys.cell(row=ri, column=chqno_col, value=chq_display).fill = F(YELLOW)
        if old_col:
            ws_sys.cell(row=ri, column=old_col,   value=round(paid_amt, 2)).fill = F(LBLUE)
        if chq_in_bank:  cnt_old  += 1
        else:            cnt_gpay += 1
    else:
        # ── NOT confirmed by bank -> Pending = invoice Amount
        if pend_col and inv_amt:
            ws_sys.cell(row=ri, column=pend_col, value=inv_amt).fill = F(RED)
        cnt_pend += 1

wb_sys.save("System_Puchase.xlsx")
print(f"  Old Account filled (CHQ confirmed)  : {cnt_old}")
print(f"  Old Account filled (GPAY confirmed) : {cnt_gpay}")
print(f"  Pending filled (not in bank)        : {cnt_pend}")
print(f"  Previous Pending filled             : {cnt_prev}")
print("  Saved: System_Puchase.xlsx")


# ── Summary ───────────────────────────────────────────────────────────────────
print("\n" + "=" * 60)
print("RECONCILIATION COMPLETE")
print("=" * 60)
print(f"  Takeover Date     : 29-May-2025")
print()
print("  BANK STATEMENT:")
print(f"    CHQ matched (in PAIDMAST)  : {cnt_bank_chq}")
print(f"    NOT_IN_SYSTEM (pre-takeover): {cnt_bank_not_sys}")
print()
print("  SYSTEM_PURCHASE:")
print(f"    Old Account - CHQ confirmed : {cnt_old}")
print(f"    Old Account - GPAY confirmed: {cnt_gpay}")
print(f"    Pending (not in bank)       : {cnt_pend}")
print(f"    Previous Pending (old bills): {cnt_prev}")
print()
print("  FILES SAVED:")
print("    DBF_Joined_Data.xlsx")
print("    Old_Bank_Statement_Shree_Seva_Medical.xlsx")
print("    System_Puchase.xlsx")
print("=" * 60)
