# Medical Billing ERP – Database Documentation

## Overview

Shree Seva Medical – Medical Billing ERP is a pharmaceutical/chemist shop management system originally built on FoxPro DBF databases. This document describes the legacy schema, business rules, and the new SQLite schema used in the modernised WPF .NET 9 application.

The system manages:
- Retail & institutional drug sales with GST
- Purchase from distributors with batch/expiry tracking
- Batch-wise FIFO stock ledger
- Account receivables and payables
- GST returns (GSTR-1, GSTR-3B)
- Patient and doctor records
- User rights and audit trail
- WhatsApp bill dispatch and auto-backup

---

## Legacy DBF Tables

### PRODUCT – Product Master
| Field | Type | Description |
|-------|------|-------------|
| PRODID | C(10) | Primary key – product code |
| PRODNM | C(60) | Product name |
| MARATHINM | C(60) | Marathi name (regional) |
| UNIT | C(10) | Unit of measure (TAB, ML, GM…) |
| PACK | N | Pack size (e.g. 10 tablets/strip) |
| COMPID | C(10) | FK → COMPANY.COMPID |
| DRUGID | C(10) | FK → DRUG.DRUGID |
| SGST | N | SGST rate % |
| CGST | N | CGST rate % |
| IGST | N | IGST rate % |
| HSNCODE | C(10) | HSN code for GST |
| ACTQTY | N | Current stock quantity |
| FIXRATE | N | Fixed sale rate flag |
| MARGIN | N | Default profit margin % |
| PRODMIN | N | Minimum stock level (alert) |
| PRODMAX | N | Maximum stock level |
| NRX | L | Non-prescription flag |
| SCHDRUG | L | Scheduled/controlled substance |
| HIGHRISK | L | High-risk drug flag |
| DELETED | L | Soft-delete flag |
| BARCODE | C(15) | Barcode / EAN |
| SALERATE | N | Default sale rate |
| MRP | N | Maximum retail price |
| PURRATE | N | Last purchase rate |

### DRUG – Drug Category Master
| Field | Type | Description |
|-------|------|-------------|
| DRUGID | C(10) | Primary key |
| DRUGNM | C(40) | Category name (e.g. Antibiotic) |
| SCHDRUG | L | Scheduled substance flag |
| HIGHRISK | L | High-risk flag |
| TBMEDICINE | L | TB medicine flag |
| STKHOLD | L | Stock-hold flag |
| DISCOUNT | N | Default discount % for this category |

### COMPANY – Manufacturer/Supplier Master
| Field | Type | Description |
|-------|------|-------------|
| COMPID | C(10) | Primary key |
| COMPNM | C(60) | Company name |
| COMPDISP | C(30) | Short/display name |
| EMAIL1-3 | C(50) | Contact emails |
| COMPORMANU | L | True=Manufacturer, False=Distributor |

### ACCOUNT – Party Master (Customers, Distributors, Banks)
| Field | Type | Description |
|-------|------|-------------|
| ACCOID | C(10) | Primary key |
| ACCONM | C(60) | Party name |
| ADD1-4 | C(40) | Address lines |
| AREA | C(30) | Area/locality |
| PHONE | C(15) | Phone |
| MOBILE | C(15) | Mobile number (used for WhatsApp) |
| GSTNO | C(15) | GST registration number |
| GROUID | C(10) | FK → GROUP.GROUID |
| CDPER | N | Cash discount % |
| BANKID | C(10) | Bank account ref |
| BANKACNO | C(20) | Bank account number |
| IFSCCODE | C(11) | IFSC code |
| DLVAL | C(30) | Drug licence number |
| PANNO | C(10) | PAN number |
| LOCKED | L | Account locked flag |
| INACTIVE | L | Inactive flag |
| DUEDAYS | N | Credit period (days) |
| OPNBAL | N | Opening balance |
| OPNDR | L | Opening balance is Debit |
| STATECODE | C(2) | GST state code |
| STATENAME | C(30) | State name |

### GROUP – Account Group Hierarchy
| Field | Type | Description |
|-------|------|-------------|
| GROUID | C(10) | Primary key |
| GROUNM | C(40) | Group name |
| LEVEL | N | Hierarchy level (1=root) |
| UNDERGRP | C(10) | Parent group ID |
| CLDBBAL | N | Closing debit balance |
| CLCRBAL | N | Closing credit balance |

Standard groups: Cash, Bank, Sundry Debtors, Sundry Creditors, Capital, Sales, Purchase, Expenses, Income, GST Payable, GST Receivable.

### PURCHMAST – Purchase Invoice Header
| Field | Type | Description |
|-------|------|-------------|
| PURVCHNO | C(12) | Voucher number (generated) |
| PURVCHDATE | D | Voucher date |
| BILLNO | C(20) | Supplier's bill number |
| BILLDATE | D | Supplier's bill date |
| ACCOID | C(10) | FK → ACCOUNT (distributor) |
| PURNETAMT | N | Net payable amount |
| TOTSGST | N | Total SGST |
| TOTCGST | N | Total CGST |
| TOTIGST | N | Total IGST |
| ITDISCAMT | N | Item-level discount amount |
| SPLDISCAMT | N | Special/cash discount amount |
| ROUND | N | Rounding adjustment |
| FINYEAR | C(4) | Financial year code (e.g. 2425) |
| CHALLAN | C(20) | Challan number |
| CHALDATE | D | Challan date |
| FREIGHT | N | Freight charges |

### PURCHTRAN – Purchase Invoice Detail
| Field | Type | Description |
|-------|------|-------------|
| PURVCHNO | C(12) | FK → PURCHMAST |
| PRODID | C(10) | FK → PRODUCT |
| BATCH | C(15) | Batch number |
| EXPIRY | C(7) | Expiry (MM/YYYY) |
| EXPDATE | D | Expiry date (1st of expiry month) |
| PURQTY | N | Quantity purchased |
| FREQTY | N | Free quantity |
| ACTRATE | N | Actual rate (before GST) |
| NETRATE | N | Net rate after discount |
| MRP | N | MRP on pack |
| SALERATE | N | Derived sale rate |
| SGST | N | SGST % |
| CGST | N | CGST % |
| IGST | N | IGST % |
| SGSTAMT | N | SGST amount |
| CGSTAMT | N | CGST amount |
| IGSTAMT | N | IGST amount |
| ITDISCAMT | N | Item discount amount |
| ITDISCPER | N | Item discount % |
| STKKEY | C(30) | Composite stock key (PRODID+BATCH) |
| SCHQTY | N | Scheme quantity |

### ALLSLTRAN – Sale Transaction (all types)
| Field | Type | Description |
|-------|------|-------------|
| SALVCHNO | C(12) | Sale voucher number |
| SALTRDTYPE | C(2) | Type: SA=Sale, CR=Credit Note, etc. |
| ACCOID | C(10) | FK → ACCOUNT (customer) |
| PRODID | C(10) | FK → PRODUCT |
| BATCH | C(15) | Batch |
| EXPIRY | C(7) | Expiry |
| SALQTY | N | Sale quantity |
| NETRATE | N | Net rate |
| MRP | N | MRP |
| SALERATE | N | Sale rate |
| ITDISCAMT | N | Item discount |
| ITDISCPER | N | Item discount % |
| SGST | N | SGST % |
| CGST | N | CGST % |
| IGST | N | IGST % |
| SGSTAMT | N | SGST amount |
| CGSTAMT | N | CGST amount |
| IGSTAMT | N | IGST amount |
| PROFIT | N | Profit on this item |
| PATIID | C(10) | FK → patient |
| DOCTID | C(10) | FK → DOCTOR |
| STKKEY | C(30) | Stock key |
| VCHDATE | D | Voucher date |
| FINYEAR | C(4) | Financial year |
| CASEDISC | N | Cash discount % applied |
| FREQTY | N | Free qty given |

### STOCK – Batch-wise Stock Ledger
| Field | Type | Description |
|-------|------|-------------|
| PRODID | C(10) | FK → PRODUCT |
| BUNIT | C(10) | Branch/godown code |
| BATCH | C(15) | Batch number |
| EXPIRY | C(7) | Expiry (MM/YYYY) |
| EXPDATE | D | Expiry date |
| ACTRATE | N | Actual purchase rate |
| NETRATE | N | Net rate after disc |
| MRP | N | MRP |
| SALERATE | N | Sale rate |
| OPNQTY | N | Opening quantity |
| PURQTY | N | Total purchased qty |
| SALQTY | N | Total sold qty |
| CRNQTY | N | Credit note return qty |
| STIQTY | N | Stock-in adjustment |
| STOQTY | N | Stock-out adjustment |
| STKKEY | C(30) | Composite key |
| GODCODE | C(10) | Godown code |

Current stock = OPNQTY + PURQTY + STIQTY + CRNQTY - SALQTY - STOQTY

### RCPTMAST / RCPTTRAN – Receipt Vouchers
Receipt master holds the voucher header; detail lines link to specific sale invoices being settled.

### PAIDMAST / PAIDTRAN – Payment Vouchers
Payment master for payments to suppliers; detail lines link to purchase bills.

### CRDBNOTE / CRDBTR – Credit/Debit Notes
CRDBNOTE is the header; CRDBTR holds item-level return details.

### DOCTOR – Doctor Master
| Field | Type | Description |
|-------|------|-------------|
| DOCTID | C(10) | Primary key |
| DOCTNM | C(50) | Doctor name |
| ADD1 | C(40) | Address |
| PHONE | C(15) | Phone |
| REGNO | C(20) | Medical registration number |
| INCPER | N | Incentive % |

### patient – Patient Master
| Field | Type | Description |
|-------|------|-------------|
| PATIID | C(10) | Primary key |
| PATINM | C(50) | Patient name |
| ADD1-2 | C(40) | Address |
| PHONE | C(15) | Phone |
| EMAIL | C(50) | Email |
| DOCTID | C(10) | Referring doctor |
| BLDGRP | C(5) | Blood group |
| DOB | D | Date of birth |

### users / rights – Users and Permissions
`users` stores login credentials. `rights` has 50+ boolean columns—one per module/action—linked to USERID.

### settings – Application Settings (200+ fields)
Voucher counters, GST rates, display options, print settings, WhatsApp API keys, backup paths.

### admin – Company/Financial Year Config
Holds the active financial year dates, company GST details, bank info, UPI ID, backup paths, email settings.

### notes – Day Closing / Cash Register
One record per shift: opening cash, closing cash, sale amount, denomination breakdown.

### journal – Journal Vouchers
Manual debit/credit entries for expenses and adjustments.

### scheme – Product Schemes
Buy-N-get-M free or percentage discount schemes per product.

### discstru – Discount Structure (Slab-wise)
Amount slabs with corresponding discount percentages.

### quotmast / quottran – Quotations
Quote header and line items; can be converted to a sale.

### smssettings / whsettings – SMS/WhatsApp Settings
API URL, API key, template IDs for bill and reminder messages.

---

## Entity-Relationship Diagram (Text)

```
DRUG ──< PRODUCT >── COMPANY
            │
            ├──< PURCHTRAN >── PURCHMAST >── ACCOUNT >── GROUP
            │
            ├──< ALLSLTRAN (sale/return) >── ACCOUNT
            │         │
            │         ├── DOCTOR
            │         └── patient
            │
            └──< STOCK (batch-wise ledger)

ACCOUNT ──< RCPTTRAN >── RCPTMAST
ACCOUNT ──< PAIDTRAN >── PAIDMAST
ACCOUNT ──< CRDBTR >── CRDBNOTE
ACCOUNT ──< journal

users ──< rights
```

---

## Key Business Workflows

### Sale Flow
1. Operator selects customer account (ACCOUNT).
2. Operator adds products by name/barcode search → system looks up STOCK for available batches (oldest first – FIFO).
3. System auto-fills Batch, Expiry, Rate, MRP from STOCK record.
4. GST is computed per line: SGST+CGST if same state; IGST if different state.
5. Item discount and scheme discount are applied.
6. Cash discount % applied at footer level.
7. Net amount calculated with rounding.
8. On Save:
   - Insert header into ALLSLTRAN (type=SA) per line.
   - Deduct SALQTY from matching STOCK batch(es) – FIFO.
   - Increment ACTQTY negatively in PRODUCT.
   - If WhatsApp enabled: format bill text → POST to WA API → log in WhatsAppLog.
9. Voucher number auto-incremented from settings counter for the financial year.

### Purchase Flow
1. Operator selects distributor (ACCOUNT where group=Sundry Creditors).
2. Enters supplier bill number and date.
3. Adds products with Batch, Expiry, Qty, Rate, MRP, SaleRate, GST%, Discount.
4. System calculates SGST/CGST or IGST per line.
5. On Save:
   - Insert PURCHMAST header.
   - Insert PURCHTRAN lines.
   - For each line: check if STOCK record exists for (PRODID+BATCH) → INSERT or UPDATE PURQTY.
   - Update PRODUCT.ACTQTY, SALERATE, MRP (if new rate > old or configured to update).
   - Increment purchase voucher counter.

### Stock Flow
- Stock is maintained batch-wise in the STOCK table.
- STKKEY = PRODID + '_' + BATCH (composite lookup key).
- On purchase: PURQTY increases.
- On sale: SALQTY increases (net deduction).
- On credit note (return from customer): CRNQTY increases (stock comes back).
- On debit note (return to supplier): STOQTY increases (stock goes out).
- Current qty = OPNQTY + PURQTY + CRNQTY + STIQTY − SALQTY − STOQTY.
- Low-stock alert fires when current qty < PRODUCT.PRODMIN.
- Expiry alert fires when EXPDATE < Today + configured months.

### Payment Flow
1. Open pending purchase bills for a distributor (unpaid/partially paid).
2. Enter payment amount, cheque/UPI reference.
3. Allocate amount across selected bills.
4. Insert PAIDMAST + PAIDTRAN.
5. Update bill settled status.

### Receipt Flow
Similar to Payment but for collecting money from customers against sale invoices.

---

## GST Calculation Logic

```
Determine transaction type:
  If seller_state_code == buyer_state_code → Intrastate
    → Apply SGST + CGST (each = GST_RATE / 2)
  Else → Interstate
    → Apply IGST (= GST_RATE)

GST slabs supported: 0%, 5%, 12%, 18%, 28%

Per line item:
  TaxableAmount = Qty × Rate − ItemDiscount
  SGST_Amount   = TaxableAmount × SGST%  / 100   (intrastate)
  CGST_Amount   = TaxableAmount × CGST%  / 100   (intrastate)
  IGST_Amount   = TaxableAmount × IGST%  / 100   (interstate)
  LineTotal     = TaxableAmount + SGST_Amount + CGST_Amount + IGST_Amount

Footer:
  GrossAmount   = Σ LineTotal (before CD)
  CashDiscount  = GrossAmount × CD% / 100
  NetAmount     = GrossAmount − CashDiscount
  RoundOff      = Round(NetAmount) − NetAmount
  BillAmount    = NetAmount + RoundOff

GST is calculated on the post-item-discount taxable value.
HSN code is mandatory for each product for GST reporting.
```

---

## Voucher Numbering System

Voucher numbers follow the pattern: `PREFIX/FINYEAR/SERIAL`  
Examples:
- Sale: `SA/2425/00001`
- Purchase: `PU/2425/00001`
- Receipt: `RC/2425/00001`
- Payment: `PD/2425/00001`
- Credit Note: `CN/2425/00001`
- Debit Note: `DN/2425/00001`
- Journal: `JV/2425/00001`

Each counter is stored in the `settings` table and incremented on every save. The financial year code is a 4-digit string (e.g., `2425` for April 2024 – March 2025).

---

## GSTR-1 Data Points
- B2B invoices (registered customers) – invoice-wise details
- B2C invoices (unregistered) – aggregate
- HSN summary – product-category-wise taxable and GST amounts
- Credit/Debit notes

## GSTR-3B Summary
- Outward supplies taxable value, SGST, CGST, IGST
- Inward supplies (ITC claim) from purchase
- Net tax payable after ITC set-off
