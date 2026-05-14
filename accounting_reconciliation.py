"""
ShreeSeva Medical Accounting Reconciliation Script
Takeover Date: 29-May-2025

Logic:
- PAIDMAST.CHQNO  → match Bank Statement Narration last digits
- PAIDMAST.PADVCHNO → link to PAIDTRAN rows
- PAIDTRAN.PURVCHNO → match System_Purchase.Vou.No.

Bank Statement new columns:
  Voucher_No  = PURVCHNO values comma-separated (from PAIDTRAN for that payment)
  Party_Name  = ACCOUNT.ACCONM

System_Purchase columns filled:
  Date           = PAIDMAST.CHQDATE
  Type           = CHQ / GPAY / NEFT etc.
  ChQ No.        = PAIDMAST.CHQNO
  Previous Pend  = sum of PURPAIDAMT where BILLDATE < 29-May-2025 (same PADVCHNO)
  Old Account    = PURPAIDAMT of the specific matched PAIDTRAN row (this Vou.No.)
"""

import struct
import re
import openpyxl
from openpyxl.styles import PatternFill, Font
from datetime import date
from collections import defaultdict

TAKEOVER_DT = date(2025, 5, 29)


# ─── DBF Reader ───────────────────────────────────────────────────────────────

def read_dbf(filename):
    with open(filename, "rb") as f:
        header = f.read(32)
        num_records = struct.unpack("<I", header[4:8])[0]
        header_size = struct.unpack("<H", header[8:10])[0]
        record_size = struct.unpack("<H", header[10:12])[0]

        fields = []
        while True:
            fd = f.read(32)
            if fd[0] == 0x0D:
                break
            name = fd[:11].replace(b"\x00", b"").decode("latin-1")
            ftype = chr(fd[11])
            flen = fd[16]
            fields.append((name, ftype, flen))

        f.seek(header_size)
        records = []
        for _ in range(num_records):
            rec = f.read(record_size)
            if not rec or rec[0] == 0x1A:
                break
            if rec[0] == ord("*"):
                continue
            row = {}
            pos = 1
            for name, ftype, flen in fields:
                row[name] = rec[pos:pos + flen].decode("latin-1").strip()
                pos += flen
            records.append(row)
    return records


def parse_date(s):
    if s and len(s) == 8 and s.isdigit():
        try:
            return date(int(s[:4]), int(s[4:6]), int(s[6:8]))
        except ValueError:
            pass
    return None


# ─── Load DBF Tables ──────────────────────────────────────────────────────────

print("=" * 60)
print("Loading DBF tables...")

account  = read_dbf("DataBase/ACCOUNT.DBF")
paidmast = read_dbf("DataBase/PAIDMAST.DBF")
paidtran = read_dbf("DataBase/PAIDTRAN.DBF")

print(f"  ACCOUNT : {len(account)} | PAIDMAST: {len(paidmast)} | PAIDTRAN: {len(paidtran)}")

# Lookup maps
acct_by_id    = {r["ACCOID"]: r for r in account}
mast_by_vch   = {r["PADVCHNO"]: r for r in paidmast}

# PADVCHNO -> list of PAIDTRAN rows
tran_by_padvch = defaultdict(list)
# PURVCHNO  -> list of PAIDTRAN rows
tran_by_purvch = defaultdict(list)

for r in paidtran:
    tran_by_padvch[r["PADVCHNO"]].append(r)
    tran_by_purvch[r["PURVCHNO"]].append(r)

# CHQ number (integer) -> PAIDMAST row
chq_lookup = {}
for r in paidmast:
    chq = r["CHQNO"].strip()
    if chq.isdigit():
        chq_lookup[int(chq)] = r

# Pre-calculate per PADVCHNO:
#   prev_pend = sum PURPAIDAMT where BILLDATE < 29-May (old bills)
prev_pend_by_padvch = defaultdict(float)
for r in paidtran:
    bd = parse_date(r["BILLDATE"])
    amt = float(r["PURPAIDAMT"]) if r["PURPAIDAMT"] else 0
    if bd and bd < TAKEOVER_DT:
        prev_pend_by_padvch[r["PADVCHNO"]] += amt


# ─── Export Joined DBF to Excel ───────────────────────────────────────────────

print("\nExporting DBF_Joined_Data.xlsx ...")

wb_dbf = openpyxl.Workbook()

# Sheet 1: PAIDMAST + ACCOUNT
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
        acct.get("ACCONM", ""),
        float(r["PADVCHAMT"]) if r["PADVCHAMT"] else 0,
        float(r["PADDISC"])   if r["PADDISC"]   else 0,
        float(r["PENDING"])   if r["PENDING"]   else 0,
        r["CHQNO"],
        parse_date(r["CHQDATE"]),
        r["PADTRDTYPE"],
        r["NARA"],
        r["FINYEAR"],
    ])

# Sheet 2: PAIDTRAN full join
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
        acct.get("ACCONM", ""),
        r["BILLNO"],
        bd,
        float(r["PURNETAMT"])  if r["PURNETAMT"]  else 0,
        float(r["PURPAIDAMT"]) if r["PURPAIDAMT"] else 0,
        float(r["PURBALAMT"])  if r["PURBALAMT"]  else 0,
        float(r["DISCOUNT"])   if r["DISCOUNT"]   else 0,
        int(r["PURVCHNO"]) if r["PURVCHNO"].isdigit() else r["PURVCHNO"],
        mast.get("CHQNO", ""),
        parse_date(mast.get("CHQDATE", "")),
        r["PADTRDTYPE"],
        "YES" if (bd and bd < TAKEOVER_DT) else "NO",
    ])

hdr_fill = PatternFill(start_color="1F4E79", end_color="1F4E79", fill_type="solid")
hdr_font = Font(color="FFFFFF", bold=True)
for ws in [ws1, ws2]:
    for cell in ws[1]:
        cell.fill = hdr_fill
        cell.font = hdr_font
    ws.freeze_panes = ws["A2"]

wb_dbf.save("DBF_Joined_Data.xlsx")
print("  Saved: DBF_Joined_Data.xlsx")


# ─── Update Old_Bank_Statement ────────────────────────────────────────────────

print("\nUpdating Old_Bank_Statement_Shree_Seva_Medical.xlsx ...")

wb_bank = openpyxl.load_workbook("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
ws_bank = wb_bank.active
bank_headers = [c.value for c in ws_bank[1]]

# Add new columns if not present
def get_or_add_col(ws, headers, name):
    if name in headers:
        return headers.index(name) + 1
    col = ws.max_column + 1
    ws.cell(row=1, column=col, value=name)
    return col

vch_col = get_or_add_col(ws_bank, bank_headers, "Voucher_No")   # PURVCHNO list
pty_col = get_or_add_col(ws_bank, bank_headers, "Party_Name")   # party name

# Style new headers
new_fill = PatternFill(start_color="FF6600", end_color="FF6600", fill_type="solid")
new_font = Font(color="FFFFFF", bold=True)
for col in [vch_col, pty_col]:
    c = ws_bank.cell(row=1, column=col)
    c.fill = new_fill
    c.font = new_font

match_fill = PatternFill(start_color="E2EFDA", end_color="E2EFDA", fill_type="solid")

matched_bank = 0
for row_idx in range(2, ws_bank.max_row + 1):
    narration = ws_bank.cell(row=row_idx, column=2).value
    if not narration:
        continue
    nums = re.findall(r"\d+", str(narration))
    if not nums:
        continue

    mast_row = chq_lookup.get(int(nums[-1]))
    if not mast_row:
        continue

    matched_bank += 1

    # Party Name
    acct = acct_by_id.get(mast_row["ACCOID"], {})
    ws_bank.cell(row=row_idx, column=pty_col, value=acct.get("ACCONM", "")).fill = match_fill

    # Voucher_No = PURVCHNO comma-separated from PAIDTRAN for this PADVCHNO
    tran_rows = tran_by_padvch.get(mast_row["PADVCHNO"], [])
    purvchno_list = ",".join(
        r["PURVCHNO"] for r in tran_rows if r["PURVCHNO"]
    )
    c = ws_bank.cell(row=row_idx, column=vch_col, value=purvchno_list)
    c.fill = match_fill

print(f"  Bank rows matched: {matched_bank}")
wb_bank.save("Old_Bank_Statement_Shree_Seva_Medical.xlsx")
print("  Saved.")


# ─── Update System_Purchase ───────────────────────────────────────────────────

print("\nUpdating System_Puchase.xlsx ...")

wb_sys = openpyxl.load_workbook("System_Puchase.xlsx")
ws_sys = wb_sys.active
sys_headers = [c.value for c in ws_sys[1]]

def col_pos(name, headers):
    for i, h in enumerate(headers):
        if h and str(h).strip().lower() == name.lower():
            return i + 1
    return None

date_col      = col_pos("Date",             sys_headers)
type_col      = col_pos("Type",             sys_headers)
chqno_col     = col_pos("ChQ No.",          sys_headers)
prevpend_col  = col_pos("Previous Pending", sys_headers)
oldacct_col   = col_pos("Old Account",      sys_headers)

print(f"  Cols → Date:{date_col} Type:{type_col} CHQ:{chqno_col} PrevPend:{prevpend_col} OldAcct:{oldacct_col}")

upd_fill  = PatternFill(start_color="FFF2CC", end_color="FFF2CC", fill_type="solid")  # yellow
prev_fill = PatternFill(start_color="FFD966", end_color="FFD966", fill_type="solid")  # gold
old_fill  = PatternFill(start_color="BDD7EE", end_color="BDD7EE", fill_type="solid")  # blue

sys_matched  = 0
sys_with_prev = 0

for row_idx in range(2, ws_sys.max_row + 1):
    vou_cell = ws_sys.cell(row=row_idx, column=1).value
    if vou_cell is None:
        continue
    vou_str = str(int(vou_cell)) if isinstance(vou_cell, (int, float)) else str(vou_cell).strip()

    # Find PAIDTRAN row(s) where PURVCHNO = this Vou.No.
    tran_rows = tran_by_purvch.get(vou_str, [])
    if not tran_rows:
        continue

    sys_matched += 1

    # Use first matched PAIDTRAN row to get PADVCHNO → PAIDMAST
    tr = tran_rows[0]
    padvchno = tr["PADVCHNO"]
    mast_row = mast_by_vch.get(padvchno, {})

    if mast_row:
        chq_no  = mast_row.get("CHQNO", "").strip()
        chq_dt  = parse_date(mast_row.get("CHQDATE", ""))

        if chq_no.upper() in ("GPAY", "NEFT", "RTGS", "UPI", "CASH"):
            display_type = chq_no.upper()
            chq_display  = ""
        else:
            display_type = "CHQ"
            chq_display  = chq_no

        if date_col:
            c = ws_sys.cell(row=row_idx, column=date_col, value=chq_dt)
            c.fill = upd_fill
        if type_col:
            c = ws_sys.cell(row=row_idx, column=type_col, value=display_type)
            c.fill = upd_fill
        if chqno_col:
            c = ws_sys.cell(row=row_idx, column=chqno_col, value=chq_display)
            c.fill = upd_fill

    # Previous Pending = sum of PURPAIDAMT for BILLDATE < 29-May (same PADVCHNO)
    prev_amt = prev_pend_by_padvch.get(padvchno, 0)
    if prev_amt > 0 and prevpend_col:
        c = ws_sys.cell(row=row_idx, column=prevpend_col, value=round(prev_amt, 2))
        c.fill = prev_fill
        sys_with_prev += 1

    # Old Account = PURPAIDAMT of THIS specific matched row (Vou.No. = PURVCHNO)
    paid_amt = float(tr["PURPAIDAMT"]) if tr["PURPAIDAMT"] else 0
    if paid_amt and oldacct_col:
        c = ws_sys.cell(row=row_idx, column=oldacct_col, value=round(paid_amt, 2))
        c.fill = old_fill

print(f"  System rows matched : {sys_matched}")
print(f"  Rows with Prev Pend : {sys_with_prev}")
wb_sys.save("System_Puchase.xlsx")
print("  Saved.")


# ─── Summary ──────────────────────────────────────────────────────────────────

print("\n" + "=" * 60)
print("DONE")
total_prev = sum(prev_pend_by_padvch.values())
print(f"  Total Previous Pending (pre-29-May): Rs. {total_prev:,.2f}")
print(f"  Bank rows matched  : {matched_bank}")
print(f"  System rows matched: {sys_matched}")
print("=" * 60)
