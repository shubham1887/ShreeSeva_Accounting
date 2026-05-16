-- ============================================================
-- Medical Billing ERP - SQLite3 Schema
-- Multi-tenant, normalized, GST-compliant
-- ============================================================
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ============================================================
-- TENANTS
-- ============================================================
CREATE TABLE IF NOT EXISTS Tenants (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT    NOT NULL,
    LicenseKey      TEXT,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now'))
);

-- ============================================================
-- DRUG CATEGORIES
-- ============================================================
CREATE TABLE IF NOT EXISTS DrugCategories (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    CategoryCode    TEXT    NOT NULL,
    CategoryName    TEXT    NOT NULL,
    IsScheduled     INTEGER NOT NULL DEFAULT 0,
    IsHighRisk      INTEGER NOT NULL DEFAULT 0,
    IsTBMedicine    INTEGER NOT NULL DEFAULT 0,
    IsStockHold     INTEGER NOT NULL DEFAULT 0,
    DefaultDiscount REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE INDEX IF NOT EXISTS IX_DrugCategories_Tenant ON DrugCategories(TenantId);

-- ============================================================
-- MANUFACTURERS / COMPANIES
-- ============================================================
CREATE TABLE IF NOT EXISTS Manufacturers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    CompanyCode     TEXT    NOT NULL,
    CompanyName     TEXT    NOT NULL,
    DisplayName     TEXT,
    Email1          TEXT,
    Email2          TEXT,
    Email3          TEXT,
    Phone           TEXT,
    IsManufacturer  INTEGER NOT NULL DEFAULT 1,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE INDEX IF NOT EXISTS IX_Manufacturers_Tenant ON Manufacturers(TenantId);

-- ============================================================
-- PRODUCTS
-- ============================================================
CREATE TABLE IF NOT EXISTS Products (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ProductCode     TEXT    NOT NULL,
    ProductName     TEXT    NOT NULL,
    MarathiName     TEXT,
    Barcode         TEXT,
    Unit            TEXT    NOT NULL DEFAULT 'NOS',
    PackSize        INTEGER NOT NULL DEFAULT 1,
    ManufacturerId  INTEGER,
    DrugCategoryId  INTEGER,
    HSNCode         TEXT,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    IsFixedRate     INTEGER NOT NULL DEFAULT 0,
    Margin          REAL    NOT NULL DEFAULT 0,
    MinQty          REAL    NOT NULL DEFAULT 0,
    MaxQty          REAL    NOT NULL DEFAULT 0,
    IsNonRx         INTEGER NOT NULL DEFAULT 1,
    IsScheduled     INTEGER NOT NULL DEFAULT 0,
    IsHighRisk      INTEGER NOT NULL DEFAULT 0,
    DefaultSaleRate REAL    NOT NULL DEFAULT 0,
    DefaultMRP      REAL    NOT NULL DEFAULT 0,
    LastPurchaseRate REAL   NOT NULL DEFAULT 0,
    CurrentQty      REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)        REFERENCES Tenants(Id),
    FOREIGN KEY (ManufacturerId)  REFERENCES Manufacturers(Id),
    FOREIGN KEY (DrugCategoryId)  REFERENCES DrugCategories(Id)
);
CREATE INDEX IF NOT EXISTS IX_Products_Tenant      ON Products(TenantId);
CREATE INDEX IF NOT EXISTS IX_Products_Name        ON Products(TenantId, ProductName);
CREATE INDEX IF NOT EXISTS IX_Products_Barcode     ON Products(TenantId, Barcode);
CREATE INDEX IF NOT EXISTS IX_Products_Category    ON Products(TenantId, DrugCategoryId);
CREATE INDEX IF NOT EXISTS IX_Products_Manufacturer ON Products(TenantId, ManufacturerId);

-- ============================================================
-- PRODUCT ALIASES (purchase aliases / wholesale names)
-- ============================================================
CREATE TABLE IF NOT EXISTS ProductAliases (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId    INTEGER NOT NULL DEFAULT 1,
    ProductId   INTEGER NOT NULL,
    AliasName   TEXT    NOT NULL,
    AliasType   TEXT    NOT NULL DEFAULT 'PURCHASE',
    IsDeleted   INTEGER NOT NULL DEFAULT 0,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (ProductId)  REFERENCES Products(Id)
);

-- ============================================================
-- ACCOUNT GROUPS
-- ============================================================
CREATE TABLE IF NOT EXISTS AccountGroups (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    GroupCode       TEXT    NOT NULL,
    GroupName       TEXT    NOT NULL,
    Level           INTEGER NOT NULL DEFAULT 1,
    ParentGroupId   INTEGER,
    NatureType      TEXT    NOT NULL DEFAULT 'NA',  -- ASSET/LIABILITY/INCOME/EXPENSE
    IsSystem        INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)      REFERENCES Tenants(Id),
    FOREIGN KEY (ParentGroupId) REFERENCES AccountGroups(Id)
);
CREATE INDEX IF NOT EXISTS IX_AccountGroups_Tenant ON AccountGroups(TenantId);

-- ============================================================
-- ACCOUNTS (Customers, Distributors, Banks, Cash)
-- ============================================================
CREATE TABLE IF NOT EXISTS Accounts (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    AccountCode     TEXT    NOT NULL,
    AccountName     TEXT    NOT NULL,
    Address1        TEXT,
    Address2        TEXT,
    Address3        TEXT,
    Address4        TEXT,
    Area            TEXT,
    City            TEXT,
    State           TEXT,
    StateCode       TEXT,
    PinCode         TEXT,
    Phone           TEXT,
    Mobile          TEXT,
    Email           TEXT,
    GSTIN           TEXT,
    GroupId         INTEGER,
    CashDiscountPer REAL    NOT NULL DEFAULT 0,
    BankName        TEXT,
    BankAccountNo   TEXT,
    IFSCCode        TEXT,
    DrugLicenseNo   TEXT,
    PANNo           TEXT,
    OpeningBalance  REAL    NOT NULL DEFAULT 0,
    OpeningDr       INTEGER NOT NULL DEFAULT 0,  -- 1=Debit, 0=Credit
    DueDays         INTEGER NOT NULL DEFAULT 0,
    IsLocked        INTEGER NOT NULL DEFAULT 0,
    IsInactive      INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (GroupId)  REFERENCES AccountGroups(Id)
);
CREATE INDEX IF NOT EXISTS IX_Accounts_Tenant  ON Accounts(TenantId);
CREATE INDEX IF NOT EXISTS IX_Accounts_Name    ON Accounts(TenantId, AccountName);
CREATE INDEX IF NOT EXISTS IX_Accounts_Group   ON Accounts(TenantId, GroupId);
CREATE INDEX IF NOT EXISTS IX_Accounts_GSTIN   ON Accounts(TenantId, GSTIN);

-- ============================================================
-- DOCTORS
-- ============================================================
CREATE TABLE IF NOT EXISTS Doctors (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    DoctorCode      TEXT    NOT NULL,
    DoctorName      TEXT    NOT NULL,
    Address         TEXT,
    Phone           TEXT,
    Mobile          TEXT,
    RegNo           TEXT,
    IncentivePer    REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE INDEX IF NOT EXISTS IX_Doctors_Tenant ON Doctors(TenantId);

-- ============================================================
-- PATIENTS
-- ============================================================
CREATE TABLE IF NOT EXISTS Patients (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    PatientCode     TEXT    NOT NULL,
    PatientName     TEXT    NOT NULL,
    Address1        TEXT,
    Address2        TEXT,
    Phone           TEXT,
    Mobile          TEXT,
    Email           TEXT,
    DoctorId        INTEGER,
    BloodGroup      TEXT,
    DateOfBirth     TEXT,
    Gender          TEXT,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)  REFERENCES Tenants(Id),
    FOREIGN KEY (DoctorId)  REFERENCES Doctors(Id)
);
CREATE INDEX IF NOT EXISTS IX_Patients_Tenant ON Patients(TenantId);
CREATE INDEX IF NOT EXISTS IX_Patients_Name   ON Patients(TenantId, PatientName);

-- ============================================================
-- STOCK (Batch-wise)
-- ============================================================
CREATE TABLE IF NOT EXISTS Stocks (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ProductId       INTEGER NOT NULL,
    BatchNo         TEXT    NOT NULL,
    ExpiryMY        TEXT    NOT NULL,  -- MM/YYYY format
    ExpiryDate      TEXT    NOT NULL,  -- ISO date of last day of expiry month
    GodownCode      TEXT    NOT NULL DEFAULT 'MAIN',
    ActualRate      REAL    NOT NULL DEFAULT 0,
    NetRate         REAL    NOT NULL DEFAULT 0,
    MRP             REAL    NOT NULL DEFAULT 0,
    SaleRate        REAL    NOT NULL DEFAULT 0,
    OpeningQty      REAL    NOT NULL DEFAULT 0,
    PurchasedQty    REAL    NOT NULL DEFAULT 0,
    SoldQty         REAL    NOT NULL DEFAULT 0,
    CreditNoteQty   REAL    NOT NULL DEFAULT 0,
    StockInQty      REAL    NOT NULL DEFAULT 0,
    StockOutQty     REAL    NOT NULL DEFAULT 0,
    StockKey        TEXT    NOT NULL,  -- ProductCode_BatchNo
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (ProductId)  REFERENCES Products(Id)
);
CREATE INDEX IF NOT EXISTS IX_Stocks_Tenant    ON Stocks(TenantId);
CREATE INDEX IF NOT EXISTS IX_Stocks_Product   ON Stocks(TenantId, ProductId);
CREATE INDEX IF NOT EXISTS IX_Stocks_StockKey  ON Stocks(TenantId, StockKey);
CREATE INDEX IF NOT EXISTS IX_Stocks_Expiry    ON Stocks(TenantId, ExpiryDate);

-- ============================================================
-- PURCHASE MASTER
-- ============================================================
CREATE TABLE IF NOT EXISTS PurchaseMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    BillNo          TEXT,
    BillDate        TEXT,
    ChallanNo       TEXT,
    ChallanDate     TEXT,
    AccountId       INTEGER NOT NULL,
    GrossAmount     REAL    NOT NULL DEFAULT 0,
    ItemDiscAmount  REAL    NOT NULL DEFAULT 0,
    SpecialDisc     REAL    NOT NULL DEFAULT 0,
    FreightAmount   REAL    NOT NULL DEFAULT 0,
    TotalSGST       REAL    NOT NULL DEFAULT 0,
    TotalCGST       REAL    NOT NULL DEFAULT 0,
    TotalIGST       REAL    NOT NULL DEFAULT 0,
    RoundOff        REAL    NOT NULL DEFAULT 0,
    NetAmount       REAL    NOT NULL DEFAULT 0,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    Narration       TEXT,
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE INDEX IF NOT EXISTS IX_PurchaseMaster_Tenant  ON PurchaseMaster(TenantId);
CREATE INDEX IF NOT EXISTS IX_PurchaseMaster_Date    ON PurchaseMaster(TenantId, VoucherDate);
CREATE INDEX IF NOT EXISTS IX_PurchaseMaster_Account ON PurchaseMaster(TenantId, AccountId);
CREATE INDEX IF NOT EXISTS IX_PurchaseMaster_VchNo   ON PurchaseMaster(TenantId, VoucherNo);
CREATE UNIQUE INDEX IF NOT EXISTS UX_PurchaseMaster_VchNo ON PurchaseMaster(TenantId, VoucherNo);

-- ============================================================
-- PURCHASE DETAIL
-- ============================================================
CREATE TABLE IF NOT EXISTS PurchaseDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    PurchaseMasterId INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    BatchNo         TEXT    NOT NULL,
    ExpiryMY        TEXT    NOT NULL,
    ExpiryDate      TEXT    NOT NULL,
    Quantity        REAL    NOT NULL DEFAULT 0,
    FreeQuantity    REAL    NOT NULL DEFAULT 0,
    SchemeQty       REAL    NOT NULL DEFAULT 0,
    ActualRate      REAL    NOT NULL DEFAULT 0,
    NetRate         REAL    NOT NULL DEFAULT 0,
    MRP             REAL    NOT NULL DEFAULT 0,
    SaleRate        REAL    NOT NULL DEFAULT 0,
    ItemDiscPer     REAL    NOT NULL DEFAULT 0,
    ItemDiscAmt     REAL    NOT NULL DEFAULT 0,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    SGSTAmount      REAL    NOT NULL DEFAULT 0,
    CGSTAmount      REAL    NOT NULL DEFAULT 0,
    IGSTAmount      REAL    NOT NULL DEFAULT 0,
    TaxableAmount   REAL    NOT NULL DEFAULT 0,
    LineTotal       REAL    NOT NULL DEFAULT 0,
    StockKey        TEXT    NOT NULL,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)          REFERENCES Tenants(Id),
    FOREIGN KEY (PurchaseMasterId)  REFERENCES PurchaseMaster(Id),
    FOREIGN KEY (ProductId)         REFERENCES Products(Id)
);
CREATE INDEX IF NOT EXISTS IX_PurchaseDetails_Master  ON PurchaseDetails(TenantId, PurchaseMasterId);
CREATE INDEX IF NOT EXISTS IX_PurchaseDetails_Product ON PurchaseDetails(TenantId, ProductId);

-- ============================================================
-- SALE MASTER
-- ============================================================
CREATE TABLE IF NOT EXISTS SaleMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    TransactionType TEXT    NOT NULL DEFAULT 'SA',  -- SA=Sale, CR=CreditNote
    AccountId       INTEGER NOT NULL,
    PatientId       INTEGER,
    DoctorId        INTEGER,
    GrossAmount     REAL    NOT NULL DEFAULT 0,
    ItemDiscAmount  REAL    NOT NULL DEFAULT 0,
    CashDiscPer     REAL    NOT NULL DEFAULT 0,
    CashDiscAmount  REAL    NOT NULL DEFAULT 0,
    TotalSGST       REAL    NOT NULL DEFAULT 0,
    TotalCGST       REAL    NOT NULL DEFAULT 0,
    TotalIGST       REAL    NOT NULL DEFAULT 0,
    RoundOff        REAL    NOT NULL DEFAULT 0,
    NetAmount       REAL    NOT NULL DEFAULT 0,
    PaymentMode     TEXT    NOT NULL DEFAULT 'CASH',  -- CASH/CREDIT/CHEQUE/UPI
    ChequeNo        TEXT,
    ChequeDate      TEXT,
    UPIRef          TEXT,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsInterState    INTEGER NOT NULL DEFAULT 0,
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id),
    FOREIGN KEY (PatientId)  REFERENCES Patients(Id),
    FOREIGN KEY (DoctorId)   REFERENCES Doctors(Id)
);
CREATE INDEX IF NOT EXISTS IX_SaleMaster_Tenant  ON SaleMaster(TenantId);
CREATE INDEX IF NOT EXISTS IX_SaleMaster_Date    ON SaleMaster(TenantId, VoucherDate);
CREATE INDEX IF NOT EXISTS IX_SaleMaster_Account ON SaleMaster(TenantId, AccountId);
CREATE INDEX IF NOT EXISTS IX_SaleMaster_VchNo   ON SaleMaster(TenantId, VoucherNo);
CREATE UNIQUE INDEX IF NOT EXISTS UX_SaleMaster_VchNo ON SaleMaster(TenantId, VoucherNo);

-- ============================================================
-- SALE DETAIL
-- ============================================================
CREATE TABLE IF NOT EXISTS SaleDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    SaleMasterId    INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    BatchNo         TEXT    NOT NULL,
    ExpiryMY        TEXT    NOT NULL,
    ExpiryDate      TEXT    NOT NULL,
    Quantity        REAL    NOT NULL DEFAULT 0,
    FreeQuantity    REAL    NOT NULL DEFAULT 0,
    SaleRate        REAL    NOT NULL DEFAULT 0,
    MRP             REAL    NOT NULL DEFAULT 0,
    ItemDiscPer     REAL    NOT NULL DEFAULT 0,
    ItemDiscAmt     REAL    NOT NULL DEFAULT 0,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    SGSTAmount      REAL    NOT NULL DEFAULT 0,
    CGSTAmount      REAL    NOT NULL DEFAULT 0,
    IGSTAmount      REAL    NOT NULL DEFAULT 0,
    TaxableAmount   REAL    NOT NULL DEFAULT 0,
    LineTotal       REAL    NOT NULL DEFAULT 0,
    PurchaseRate    REAL    NOT NULL DEFAULT 0,
    Profit          REAL    NOT NULL DEFAULT 0,
    StockKey        TEXT    NOT NULL,
    StockId         INTEGER,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)      REFERENCES Tenants(Id),
    FOREIGN KEY (SaleMasterId)  REFERENCES SaleMaster(Id),
    FOREIGN KEY (ProductId)     REFERENCES Products(Id),
    FOREIGN KEY (StockId)       REFERENCES Stocks(Id)
);
CREATE INDEX IF NOT EXISTS IX_SaleDetails_Master  ON SaleDetails(TenantId, SaleMasterId);
CREATE INDEX IF NOT EXISTS IX_SaleDetails_Product ON SaleDetails(TenantId, ProductId);

-- ============================================================
-- RECEIPT MASTER
-- ============================================================
CREATE TABLE IF NOT EXISTS ReceiptMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    Amount          REAL    NOT NULL DEFAULT 0,
    PaymentMode     TEXT    NOT NULL DEFAULT 'CASH',
    ChequeNo        TEXT,
    ChequeDate      TEXT,
    OurBankId       INTEGER,
    ReceivedDate    TEXT,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE INDEX IF NOT EXISTS IX_ReceiptMaster_Tenant  ON ReceiptMaster(TenantId);
CREATE INDEX IF NOT EXISTS IX_ReceiptMaster_Account ON ReceiptMaster(TenantId, AccountId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_ReceiptMaster_VchNo ON ReceiptMaster(TenantId, VoucherNo);

-- ============================================================
-- RECEIPT DETAIL (Bill-wise allocation)
-- ============================================================
CREATE TABLE IF NOT EXISTS ReceiptDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ReceiptMasterId INTEGER NOT NULL,
    SaleMasterId    INTEGER,
    AllocatedAmount REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)        REFERENCES Tenants(Id),
    FOREIGN KEY (ReceiptMasterId) REFERENCES ReceiptMaster(Id),
    FOREIGN KEY (SaleMasterId)    REFERENCES SaleMaster(Id)
);

-- ============================================================
-- PAYMENT MASTER
-- ============================================================
CREATE TABLE IF NOT EXISTS PaymentMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    Amount          REAL    NOT NULL DEFAULT 0,
    PaymentMode     TEXT    NOT NULL DEFAULT 'CASH',
    ChequeNo        TEXT,
    ChequeDate      TEXT,
    OurBankId       INTEGER,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE INDEX IF NOT EXISTS IX_PaymentMaster_Tenant  ON PaymentMaster(TenantId);
CREATE INDEX IF NOT EXISTS IX_PaymentMaster_Account ON PaymentMaster(TenantId, AccountId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_PaymentMaster_VchNo ON PaymentMaster(TenantId, VoucherNo);

-- ============================================================
-- PAYMENT DETAIL
-- ============================================================
CREATE TABLE IF NOT EXISTS PaymentDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    PaymentMasterId INTEGER NOT NULL,
    PurchaseMasterId INTEGER,
    AllocatedAmount REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)          REFERENCES Tenants(Id),
    FOREIGN KEY (PaymentMasterId)   REFERENCES PaymentMaster(Id),
    FOREIGN KEY (PurchaseMasterId)  REFERENCES PurchaseMaster(Id)
);

-- ============================================================
-- CREDIT NOTE MASTER (Sales return)
-- ============================================================
CREATE TABLE IF NOT EXISTS CreditNoteMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    RefVoucherNo    TEXT,
    GrossAmount     REAL    NOT NULL DEFAULT 0,
    TotalSGST       REAL    NOT NULL DEFAULT 0,
    TotalCGST       REAL    NOT NULL DEFAULT 0,
    TotalIGST       REAL    NOT NULL DEFAULT 0,
    NetAmount       REAL    NOT NULL DEFAULT 0,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_CreditNoteMaster_VchNo ON CreditNoteMaster(TenantId, VoucherNo);

-- ============================================================
-- CREDIT NOTE DETAIL
-- ============================================================
CREATE TABLE IF NOT EXISTS CreditNoteDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    CreditNoteMasterId INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    BatchNo         TEXT    NOT NULL,
    ExpiryMY        TEXT    NOT NULL,
    ReturnQty       REAL    NOT NULL DEFAULT 0,
    SaleRate        REAL    NOT NULL DEFAULT 0,
    MRP             REAL    NOT NULL DEFAULT 0,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    SGSTAmount      REAL    NOT NULL DEFAULT 0,
    CGSTAmount      REAL    NOT NULL DEFAULT 0,
    IGSTAmount      REAL    NOT NULL DEFAULT 0,
    TaxableAmount   REAL    NOT NULL DEFAULT 0,
    LineTotal       REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)            REFERENCES Tenants(Id),
    FOREIGN KEY (CreditNoteMasterId)  REFERENCES CreditNoteMaster(Id),
    FOREIGN KEY (ProductId)           REFERENCES Products(Id)
);

-- ============================================================
-- DEBIT NOTE MASTER (Purchase return)
-- ============================================================
CREATE TABLE IF NOT EXISTS DebitNoteMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    RefVoucherNo    TEXT,
    GrossAmount     REAL    NOT NULL DEFAULT 0,
    TotalSGST       REAL    NOT NULL DEFAULT 0,
    TotalCGST       REAL    NOT NULL DEFAULT 0,
    TotalIGST       REAL    NOT NULL DEFAULT 0,
    NetAmount       REAL    NOT NULL DEFAULT 0,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_DebitNoteMaster_VchNo ON DebitNoteMaster(TenantId, VoucherNo);

-- ============================================================
-- DEBIT NOTE DETAIL
-- ============================================================
CREATE TABLE IF NOT EXISTS DebitNoteDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    DebitNoteMasterId INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    BatchNo         TEXT    NOT NULL,
    ExpiryMY        TEXT    NOT NULL,
    ReturnQty       REAL    NOT NULL DEFAULT 0,
    PurchaseRate    REAL    NOT NULL DEFAULT 0,
    MRP             REAL    NOT NULL DEFAULT 0,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    SGSTAmount      REAL    NOT NULL DEFAULT 0,
    CGSTAmount      REAL    NOT NULL DEFAULT 0,
    IGSTAmount      REAL    NOT NULL DEFAULT 0,
    TaxableAmount   REAL    NOT NULL DEFAULT 0,
    LineTotal       REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)           REFERENCES Tenants(Id),
    FOREIGN KEY (DebitNoteMasterId)  REFERENCES DebitNoteMaster(Id),
    FOREIGN KEY (ProductId)          REFERENCES Products(Id)
);

-- ============================================================
-- JOURNAL VOUCHERS
-- ============================================================
CREATE TABLE IF NOT EXISTS JournalVouchers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    DebitAmount     REAL    NOT NULL DEFAULT 0,
    CreditAmount    REAL    NOT NULL DEFAULT 0,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsCancelled     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE INDEX IF NOT EXISTS IX_JournalVouchers_Tenant  ON JournalVouchers(TenantId);
CREATE INDEX IF NOT EXISTS IX_JournalVouchers_Date    ON JournalVouchers(TenantId, VoucherDate);
CREATE UNIQUE INDEX IF NOT EXISTS UX_JournalVouchers_VchNo ON JournalVouchers(TenantId, VoucherNo);

-- ============================================================
-- PRODUCT SCHEMES
-- ============================================================
CREATE TABLE IF NOT EXISTS ProductSchemes (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ProductId       INTEGER NOT NULL,
    BuyQty1         REAL    NOT NULL DEFAULT 0,
    FreeQty1        REAL    NOT NULL DEFAULT 0,
    DiscPer1        REAL    NOT NULL DEFAULT 0,
    BuyQty2         REAL    NOT NULL DEFAULT 0,
    FreeQty2        REAL    NOT NULL DEFAULT 0,
    DiscPer2        REAL    NOT NULL DEFAULT 0,
    BuyQty3         REAL    NOT NULL DEFAULT 0,
    FreeQty3        REAL    NOT NULL DEFAULT 0,
    DiscPer3        REAL    NOT NULL DEFAULT 0,
    ValidFrom       TEXT,
    ValidTo         TEXT,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (ProductId)  REFERENCES Products(Id)
);

-- ============================================================
-- DISCOUNT STRUCTURE (Amount-slab based)
-- ============================================================
CREATE TABLE IF NOT EXISTS DiscountStructure (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ToAmount        REAL    NOT NULL DEFAULT 0,
    DiscountPer     REAL    NOT NULL DEFAULT 0,
    ProfitLow       REAL    NOT NULL DEFAULT 0,
    ProfitHigh      REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

-- ============================================================
-- VOUCHER SERIES / COUNTERS
-- ============================================================
CREATE TABLE IF NOT EXISTS VoucherSeries (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherType     TEXT    NOT NULL,  -- SALE/PURCHASE/RECEIPT/PAYMENT/CN/DN/JOURNAL
    Prefix          TEXT    NOT NULL,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    CurrentNo       INTEGER NOT NULL DEFAULT 0,
    Padding         INTEGER NOT NULL DEFAULT 5,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_VoucherSeries ON VoucherSeries(TenantId, VoucherType, FinancialYear);

-- ============================================================
-- DAY CLOSING / CASH REGISTER
-- ============================================================
CREATE TABLE IF NOT EXISTS DayClosings (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ClosingDate     TEXT    NOT NULL,
    ShiftStart      TEXT,
    ShiftClose      TEXT,
    OpeningCash     REAL    NOT NULL DEFAULT 0,
    CashSales       REAL    NOT NULL DEFAULT 0,
    ClosingCash     REAL    NOT NULL DEFAULT 0,
    Notes           TEXT,
    Denom2000       INTEGER NOT NULL DEFAULT 0,
    Denom500        INTEGER NOT NULL DEFAULT 0,
    Denom200        INTEGER NOT NULL DEFAULT 0,
    Denom100        INTEGER NOT NULL DEFAULT 0,
    Denom50         INTEGER NOT NULL DEFAULT 0,
    Denom20         INTEGER NOT NULL DEFAULT 0,
    Denom10         INTEGER NOT NULL DEFAULT 0,
    Denom5          INTEGER NOT NULL DEFAULT 0,
    Denom2          INTEGER NOT NULL DEFAULT 0,
    Denom1          INTEGER NOT NULL DEFAULT 0,
    OperatorId      INTEGER,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

-- ============================================================
-- USERS
-- ============================================================
CREATE TABLE IF NOT EXISTS Users (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    UserCode        TEXT    NOT NULL,
    UserName        TEXT    NOT NULL,
    PasswordHash    TEXT    NOT NULL,
    Mobile          TEXT,
    Email           TEXT,
    JoinDate        TEXT,
    IsAdmin         INTEGER NOT NULL DEFAULT 0,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Code ON Users(TenantId, UserCode);

-- ============================================================
-- USER RIGHTS
-- ============================================================
CREATE TABLE IF NOT EXISTS UserRights (
    Id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId                INTEGER NOT NULL DEFAULT 1,
    UserId                  INTEGER NOT NULL,
    CanSale                 INTEGER NOT NULL DEFAULT 1,
    CanSaleEdit             INTEGER NOT NULL DEFAULT 0,
    CanSaleDelete           INTEGER NOT NULL DEFAULT 0,
    CanPurchase             INTEGER NOT NULL DEFAULT 1,
    CanPurchaseEdit         INTEGER NOT NULL DEFAULT 0,
    CanPurchaseDelete       INTEGER NOT NULL DEFAULT 0,
    CanReceipt              INTEGER NOT NULL DEFAULT 1,
    CanPayment              INTEGER NOT NULL DEFAULT 1,
    CanCreditNote           INTEGER NOT NULL DEFAULT 1,
    CanDebitNote            INTEGER NOT NULL DEFAULT 1,
    CanJournal              INTEGER NOT NULL DEFAULT 0,
    CanStockAdjust          INTEGER NOT NULL DEFAULT 0,
    CanProductMaster        INTEGER NOT NULL DEFAULT 1,
    CanAccountMaster        INTEGER NOT NULL DEFAULT 1,
    CanDoctorMaster         INTEGER NOT NULL DEFAULT 1,
    CanPatientMaster        INTEGER NOT NULL DEFAULT 1,
    CanReports              INTEGER NOT NULL DEFAULT 1,
    CanGSTReports           INTEGER NOT NULL DEFAULT 1,
    CanUserMgmt             INTEGER NOT NULL DEFAULT 0,
    CanSettings             INTEGER NOT NULL DEFAULT 0,
    CanBackup               INTEGER NOT NULL DEFAULT 0,
    CanDayClose             INTEGER NOT NULL DEFAULT 1,
    CanViewCost             INTEGER NOT NULL DEFAULT 0,
    CanChangeRate           INTEGER NOT NULL DEFAULT 1,
    CanGiveDiscount         INTEGER NOT NULL DEFAULT 1,
    MaxDiscountPer          REAL    NOT NULL DEFAULT 100,
    IsDeleted               INTEGER NOT NULL DEFAULT 0,
    CreatedAt               TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt               TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    FOREIGN KEY (UserId)   REFERENCES Users(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_UserRights_User ON UserRights(TenantId, UserId);

-- ============================================================
-- APP SETTINGS (Key-Value + structured)
-- ============================================================
CREATE TABLE IF NOT EXISTS AppSettings (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    SettingKey      TEXT    NOT NULL,
    SettingValue    TEXT,
    Category        TEXT    NOT NULL DEFAULT 'GENERAL',
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_AppSettings_Key ON AppSettings(TenantId, SettingKey);

-- ============================================================
-- COMPANY PROFILE
-- ============================================================
CREATE TABLE IF NOT EXISTS CompanyProfile (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    CompanyName     TEXT    NOT NULL,
    Address1        TEXT,
    Address2        TEXT,
    City            TEXT,
    State           TEXT,
    StateCode       TEXT,
    PinCode         TEXT,
    Phone           TEXT,
    Mobile          TEXT,
    Email           TEXT,
    Website         TEXT,
    GSTIN           TEXT,
    DrugLicense     TEXT,
    PAN             TEXT,
    BankName        TEXT,
    BankAccountNo   TEXT,
    IFSCCode        TEXT,
    UPIId           TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    YearStart       TEXT,
    YearEnd         TEXT,
    LogoPath        TEXT,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

-- ============================================================
-- QUOTATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS QuotationMaster (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    VoucherNo       TEXT    NOT NULL,
    VoucherDate     TEXT    NOT NULL,
    AccountId       INTEGER NOT NULL,
    ValidDays       INTEGER NOT NULL DEFAULT 7,
    NetAmount       REAL    NOT NULL DEFAULT 0,
    IsConverted     INTEGER NOT NULL DEFAULT 0,
    ConvertedVchNo  TEXT,
    Narration       TEXT,
    FinancialYear   TEXT    NOT NULL DEFAULT '2425',
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);

CREATE TABLE IF NOT EXISTS QuotationDetails (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    QuotationMasterId INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    Quantity        REAL    NOT NULL DEFAULT 0,
    SaleRate        REAL    NOT NULL DEFAULT 0,
    DiscPer         REAL    NOT NULL DEFAULT 0,
    SGSTRate        REAL    NOT NULL DEFAULT 0,
    CGSTRate        REAL    NOT NULL DEFAULT 0,
    IGSTRate        REAL    NOT NULL DEFAULT 0,
    LineTotal       REAL    NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)            REFERENCES Tenants(Id),
    FOREIGN KEY (QuotationMasterId)   REFERENCES QuotationMaster(Id),
    FOREIGN KEY (ProductId)           REFERENCES Products(Id)
);

-- ============================================================
-- REMINDERS / CALENDAR
-- ============================================================
CREATE TABLE IF NOT EXISTS Reminders (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    Title           TEXT    NOT NULL,
    Description     TEXT,
    ReminderDate    TEXT    NOT NULL,
    IsCompleted     INTEGER NOT NULL DEFAULT 0,
    Priority        TEXT    NOT NULL DEFAULT 'MEDIUM',
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    UpdatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

-- ============================================================
-- WHATSAPP LOG
-- ============================================================
CREATE TABLE IF NOT EXISTS WhatsAppLogs (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    Mobile          TEXT    NOT NULL,
    MessageType     TEXT    NOT NULL,  -- BILL/REMINDER/CUSTOM
    MessageText     TEXT,
    VoucherNo       TEXT,
    AccountId       INTEGER,
    SentAt          TEXT,
    IsSuccess       INTEGER NOT NULL DEFAULT 0,
    ErrorMessage    TEXT,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)  REFERENCES Accounts(Id)
);
CREATE INDEX IF NOT EXISTS IX_WhatsAppLogs_Tenant ON WhatsAppLogs(TenantId);

-- ============================================================
-- PARTY-COMPANY MAPPING (which companies does a distributor supply)
-- ============================================================
CREATE TABLE IF NOT EXISTS PartyCompanyMap (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    AccountId       INTEGER NOT NULL,
    ManufacturerId  INTEGER NOT NULL,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)        REFERENCES Tenants(Id),
    FOREIGN KEY (AccountId)       REFERENCES Accounts(Id),
    FOREIGN KEY (ManufacturerId)  REFERENCES Manufacturers(Id)
);

-- ============================================================
-- SHORTLIST (Order list / want list)
-- ============================================================
CREATE TABLE IF NOT EXISTS ShortList (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TenantId        INTEGER NOT NULL DEFAULT 1,
    ProductId       INTEGER NOT NULL,
    RequiredQty     REAL    NOT NULL DEFAULT 0,
    Notes           TEXT,
    AddedDate       TEXT    NOT NULL DEFAULT (date('now')),
    IsPurchased     INTEGER NOT NULL DEFAULT 0,
    IsDeleted       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (TenantId)   REFERENCES Tenants(Id),
    FOREIGN KEY (ProductId)  REFERENCES Products(Id)
);

-- ============================================================
-- SEED DATA – Default tenant and voucher series
-- ============================================================
INSERT OR IGNORE INTO Tenants(Id, Name, LicenseKey) VALUES (1, 'Shree Seva Medical', '');

INSERT OR IGNORE INTO VoucherSeries(TenantId, VoucherType, Prefix, FinancialYear, CurrentNo, Padding)
VALUES
    (1, 'SALE',     'SA', '2425', 0, 5),
    (1, 'PURCHASE', 'PU', '2425', 0, 5),
    (1, 'RECEIPT',  'RC', '2425', 0, 5),
    (1, 'PAYMENT',  'PD', '2425', 0, 5),
    (1, 'CN',       'CN', '2425', 0, 5),
    (1, 'DN',       'DN', '2425', 0, 5),
    (1, 'JOURNAL',  'JV', '2425', 0, 5);

INSERT OR IGNORE INTO AccountGroups(TenantId, GroupCode, GroupName, Level, NatureType, IsSystem)
VALUES
    (1, 'CASH',     'Cash',              1, 'ASSET',     1),
    (1, 'BANK',     'Bank Accounts',     1, 'ASSET',     1),
    (1, 'DEBTOR',   'Sundry Debtors',    1, 'ASSET',     1),
    (1, 'CREDITOR', 'Sundry Creditors',  1, 'LIABILITY', 1),
    (1, 'CAPITAL',  'Capital Account',   1, 'LIABILITY', 1),
    (1, 'SALES',    'Sales Account',     1, 'INCOME',    1),
    (1, 'PURCHASE', 'Purchase Account',  1, 'EXPENSE',   1),
    (1, 'EXPENSE',  'Indirect Expenses', 1, 'EXPENSE',   1),
    (1, 'INCOME',   'Indirect Income',   1, 'INCOME',    1),
    (1, 'GSTPAY',   'GST Payable',       1, 'LIABILITY', 1),
    (1, 'GSTREC',   'GST Receivable',    1, 'ASSET',     1);

-- Default Cash account
INSERT OR IGNORE INTO Accounts(TenantId, AccountCode, AccountName, GroupId)
SELECT 1, 'CASH001', 'Cash', Id FROM AccountGroups WHERE GroupCode='CASH' AND TenantId=1;

-- Default admin user (password: admin123 – SHA256 hashed)
INSERT OR IGNORE INTO Users(TenantId, UserCode, UserName, PasswordHash, IsAdmin, IsActive)
VALUES (1, 'ADMIN', 'Administrator', '240be518fabd2724ddb6f04eeb1da5967448d7e831d729d4c85d47e3bd9c03c5', 1, 1);
