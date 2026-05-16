using Medital_Application.Data;
using Medital_Application.Repositories.Interfaces;
using Medital_Application.Requests;
using Medital_Application.Responses;
using Microsoft.Data.Sqlite;

namespace Medital_Application.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _db;

    public ReportRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<SaleResponse>> GetSalesReportAsync(DateRangeRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT sm.Id, sm.VoucherNo, sm.SaleDate, a.AccountName,
                   sm.GrossAmount, sm.DiscountAmount, sm.TaxableAmount,
                   sm.SGSTAmount, sm.CGSTAmount, sm.IGSTAmount, sm.RoundOff, sm.NetAmount,
                   sm.PaymentMode, sm.IsInterState
            FROM SaleMaster sm
            LEFT JOIN Accounts a ON sm.AccountId = a.Id
            WHERE sm.TenantId = @tid AND sm.IsDeleted = 0
              AND sm.SaleDate BETWEEN @from AND @to
            ORDER BY sm.SaleDate DESC, sm.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", request.FromDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", request.ToDate.ToString("yyyy-MM-dd"));
        var list = new List<SaleResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new SaleResponse
            {
                SaleId = reader.GetInt32(0),
                VoucherNo = reader.GetString(1),
                VoucherDate = DateTime.Parse(reader.GetString(2)),
                AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                GrossAmount = reader.GetDecimal(4),
                ItemDiscAmount = reader.GetDecimal(5),
                TotalSGST = reader.GetDecimal(8),
                TotalCGST = reader.GetDecimal(9),
                TotalIGST = reader.GetDecimal(10),
                RoundOff = reader.GetDecimal(11),
                NetAmount = reader.GetDecimal(12),
                PaymentMode = reader.GetString(13),
            });
        return list;
    }

    public async Task<List<PurchaseResponse>> GetPurchaseReportAsync(DateRangeRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT pm.Id, pm.VoucherNo, pm.PurchaseDate, a.AccountName, pm.InvoiceNo,
                   pm.GrossAmount, pm.DiscountAmount, pm.SGSTAmount, pm.CGSTAmount, pm.IGSTAmount, pm.NetAmount
            FROM PurchaseMaster pm
            LEFT JOIN Accounts a ON pm.AccountId = a.Id
            WHERE pm.TenantId = @tid AND pm.IsDeleted = 0
              AND pm.PurchaseDate BETWEEN @from AND @to
            ORDER BY pm.PurchaseDate DESC, pm.Id DESC";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", request.FromDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", request.ToDate.ToString("yyyy-MM-dd"));
        var list = new List<PurchaseResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new PurchaseResponse
            {
                PurchaseId = reader.GetInt32(0),
                VoucherNo = reader.GetString(1),
                VoucherDate = DateTime.Parse(reader.GetString(2)),
                AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                BillNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                GrossAmount = reader.GetDecimal(5),
                ItemDiscAmount = reader.GetDecimal(6),
                TotalSGST = reader.GetDecimal(7),
                TotalCGST = reader.GetDecimal(8),
                TotalIGST = reader.GetDecimal(9),
                NetAmount = reader.GetDecimal(10),
            });
        return list;
    }

    public async Task<List<StockResponse>> GetStockReportAsync(string? searchTerm = null)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT s.Id, s.ProductId, p.ProductName, p.HSNCode, m.ManufacturerName,
                   s.BatchNo, s.ExpiryMY, s.ExpiryDate, s.CurrentQty, s.ActualRate, s.MRP, s.SaleRate
            FROM Stocks s
            JOIN Products p ON s.ProductId = p.Id
            LEFT JOIN Manufacturers m ON p.ManufacturerId = m.Id
            WHERE s.TenantId = @tid AND s.IsDeleted = 0 AND s.CurrentQty > 0";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            cmd.CommandText += " AND p.ProductName LIKE @search";
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
        }
        cmd.CommandText += " ORDER BY p.ProductName, s.ExpiryDate";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        var list = new List<StockResponse>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var expiryDate = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7));
            var status = "GOOD";
            if (expiryDate.HasValue)
            {
                if (expiryDate.Value <= DateTime.Today) status = "EXPIRED";
                else if (expiryDate.Value <= DateTime.Today.AddMonths(3)) status = "EXPIRING_SOON";
            }
            list.Add(new StockResponse
            {
                StockId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                ProductName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                HSNCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                ManufacturerName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                BatchNo = reader.GetString(5),
                ExpiryMY = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ExpiryDate = expiryDate,
                CurrentQty = reader.GetDecimal(8),
                ActualRate = reader.GetDecimal(9),
                MRP = reader.GetDecimal(10),
                SaleRate = reader.GetDecimal(11),
                ExpiryStatus = status,
            });
        }
        return list;
    }

    public async Task<GSTReportResponse> GetGSTReportAsync(DateRangeRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var response = new GSTReportResponse
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            B2BInvoices = new List<GSTBillItem>(),
            B2CInvoices = new List<GSTBillItem>(),
            HSNSummaries = new List<HSNSummary>()
        };

        // Sales bills
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT sm.VoucherNo, sm.SaleDate, a.AccountName, a.GSTIN,
                   sm.TaxableAmount, sm.SGSTAmount, sm.CGSTAmount, sm.IGSTAmount, sm.NetAmount,
                   sm.IsInterState
            FROM SaleMaster sm
            LEFT JOIN Accounts a ON sm.AccountId = a.Id
            WHERE sm.TenantId = @tid AND sm.IsDeleted = 0
              AND sm.SaleDate BETWEEN @from AND @to
            ORDER BY sm.SaleDate";
        cmd.Parameters.AddWithValue("@tid", _db.TenantId);
        cmd.Parameters.AddWithValue("@from", request.FromDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@to", request.ToDate.ToString("yyyy-MM-dd"));
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var gstin = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var bill = new GSTBillItem
                {
                    VoucherNo = reader.GetString(0),
                    VoucherDate = DateTime.Parse(reader.GetString(1)),
                    AccountName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    GSTIN = gstin,
                    TaxableAmount = reader.GetDecimal(4),
                    SGSTAmount = reader.GetDecimal(5),
                    CGSTAmount = reader.GetDecimal(6),
                    IGSTAmount = reader.GetDecimal(7),
                    TotalAmount = reader.GetDecimal(8),
                };
                if (!string.IsNullOrEmpty(gstin))
                    response.B2BInvoices.Add(bill);
                else
                    response.B2CInvoices.Add(bill);
            }
        }

        // HSN summary from sale details
        var hcmd = conn.CreateCommand();
        hcmd.CommandText = @"
            SELECT p.HSNCode, p.ProductName,
                   SUM(sd.Qty) AS TotalQty,
                   SUM(sd.TaxableAmount) AS TaxableAmount,
                   SUM(sd.SGSTAmount) AS SGSTAmount,
                   SUM(sd.CGSTAmount) AS CGSTAmount,
                   SUM(sd.IGSTAmount) AS IGSTAmount,
                   (SUM(sd.SGSTAmount) + SUM(sd.CGSTAmount) + SUM(sd.IGSTAmount)) AS TotalTax
            FROM SaleDetails sd
            JOIN SaleMaster sm ON sd.SaleId = sm.Id
            JOIN Products p ON sd.ProductId = p.Id
            WHERE sd.TenantId = @tid
              AND sm.SaleDate BETWEEN @from AND @to AND sm.IsDeleted = 0
            GROUP BY p.HSNCode
            ORDER BY p.HSNCode";
        hcmd.Parameters.AddWithValue("@tid", _db.TenantId);
        hcmd.Parameters.AddWithValue("@from", request.FromDate.ToString("yyyy-MM-dd"));
        hcmd.Parameters.AddWithValue("@to", request.ToDate.ToString("yyyy-MM-dd"));
        using (var reader = await hcmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                response.HSNSummaries.Add(new HSNSummary
                {
                    HSNCode = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Description = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    TotalQty = reader.GetDecimal(2),
                    TaxableAmount = reader.GetDecimal(3),
                    SGSTAmount = reader.GetDecimal(4),
                    CGSTAmount = reader.GetDecimal(5),
                    IGSTAmount = reader.GetDecimal(6),
                    TotalTax = reader.GetDecimal(7),
                });
        }

        var allBills = response.B2BInvoices.Concat(response.B2CInvoices).ToList();
        response.TotalTaxable = allBills.Sum(b => b.TaxableAmount);
        response.TotalSGST = allBills.Sum(b => b.SGSTAmount);
        response.TotalCGST = allBills.Sum(b => b.CGSTAmount);
        response.TotalIGST = allBills.Sum(b => b.IGSTAmount);
        response.TotalGST = response.TotalSGST + response.TotalCGST + response.TotalIGST;
        response.TotalSales = allBills.Sum(b => b.TotalAmount);
        response.Period = $"{request.FromDate:MMM-yyyy} to {request.ToDate:MMM-yyyy}";

        return response;
    }

    public async Task<ProfitLossResponse> GetProfitLossAsync(DateRangeRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        async Task<decimal> ScalarAsync(string sql, params (string, object)[] ps)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
            var r = await c.ExecuteScalarAsync();
            return r == null || r == DBNull.Value ? 0 : Convert.ToDecimal(r);
        }

        var grossSales = await ScalarAsync(
            "SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster WHERE TenantId=@tid AND IsDeleted=0 AND SaleDate BETWEEN @f AND @t",
            ("@tid", _db.TenantId), ("@f", request.FromDate.ToString("yyyy-MM-dd")), ("@t", request.ToDate.ToString("yyyy-MM-dd")));

        var purchases = await ScalarAsync(
            "SELECT COALESCE(SUM(NetAmount),0) FROM PurchaseMaster WHERE TenantId=@tid AND IsDeleted=0 AND PurchaseDate BETWEEN @f AND @t",
            ("@tid", _db.TenantId), ("@f", request.FromDate.ToString("yyyy-MM-dd")), ("@t", request.ToDate.ToString("yyyy-MM-dd")));

        var salesReturns = await ScalarAsync(
            "SELECT COALESCE(SUM(NetAmount),0) FROM CreditNoteMaster WHERE TenantId=@tid AND IsDeleted=0 AND VoucherDate BETWEEN @f AND @t",
            ("@tid", _db.TenantId), ("@f", request.FromDate.ToString("yyyy-MM-dd")), ("@t", request.ToDate.ToString("yyyy-MM-dd")));

        var purchaseReturns = await ScalarAsync(
            "SELECT COALESCE(SUM(NetAmount),0) FROM DebitNoteMaster WHERE TenantId=@tid AND IsDeleted=0 AND VoucherDate BETWEEN @f AND @t",
            ("@tid", _db.TenantId), ("@f", request.FromDate.ToString("yyyy-MM-dd")), ("@t", request.ToDate.ToString("yyyy-MM-dd")));

        var netSales = grossSales - salesReturns;
        var netPurchases = purchases - purchaseReturns;
        var grossProfit = netSales - netPurchases;
        var grossProfitPer = netSales > 0 ? Math.Round(grossProfit / netSales * 100, 2) : 0;

        return new ProfitLossResponse
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Period = $"{request.FromDate:dd-MMM-yyyy} to {request.ToDate:dd-MMM-yyyy}",
            GrossSales = grossSales,
            SalesReturns = salesReturns,
            NetSales = netSales,
            Purchases = purchases,
            PurchaseReturns = purchaseReturns,
            CostOfGoodsSold = netPurchases,
            GrossProfit = grossProfit,
            GrossProfitPer = grossProfitPer,
            NetProfit = grossProfit,
            NetProfitPer = grossProfitPer,
        };
    }

    public async Task<AccountLedgerResponse> GetLedgerAsync(int accountId, DateRangeRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        // Get account info
        var ac = conn.CreateCommand();
        ac.CommandText = "SELECT AccountName, OpeningBalance, OpeningBalanceType FROM Accounts WHERE Id=@id AND TenantId=@tid";
        ac.Parameters.AddWithValue("@id", accountId);
        ac.Parameters.AddWithValue("@tid", _db.TenantId);
        string accountName = "";
        decimal opening = 0; bool openingIsDebit = true;
        using (var r = await ac.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
            {
                accountName = r.GetString(0);
                opening = r.GetDecimal(1);
                openingIsDebit = r.GetString(2) == "Dr";
            }
        }

        var transactions = new List<LedgerTransaction>();

        async Task LoadTxns(string sql, string txnType, bool isDebit)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            c.Parameters.AddWithValue("@aid", accountId);
            c.Parameters.AddWithValue("@tid", _db.TenantId);
            c.Parameters.AddWithValue("@f", request.FromDate.ToString("yyyy-MM-dd"));
            c.Parameters.AddWithValue("@t", request.ToDate.ToString("yyyy-MM-dd"));
            using var r = await c.ExecuteReaderAsync();
            while (await r.ReadAsync())
                transactions.Add(new LedgerTransaction
                {
                    VoucherNo = r.GetString(0),
                    Date = DateTime.Parse(r.GetString(1)),
                    Type = txnType,
                    Narration = r.IsDBNull(3) ? txnType : r.GetString(3),
                    Debit = isDebit ? r.GetDecimal(2) : 0,
                    Credit = isDebit ? 0 : r.GetDecimal(2),
                    Balance = 0,
                    IsDebitBalance = true,
                });
        }

        await LoadTxns(@"SELECT VoucherNo, SaleDate, NetAmount, Narration FROM SaleMaster
            WHERE AccountId=@aid AND TenantId=@tid AND IsDeleted=0 AND SaleDate BETWEEN @f AND @t", "Sale", true);
        await LoadTxns(@"SELECT VoucherNo, VoucherDate, Amount, Narration FROM ReceiptMaster
            WHERE AccountId=@aid AND TenantId=@tid AND IsDeleted=0 AND VoucherDate BETWEEN @f AND @t", "Receipt", false);
        await LoadTxns(@"SELECT VoucherNo, VoucherDate, NetAmount, Narration FROM CreditNoteMaster
            WHERE AccountId=@aid AND TenantId=@tid AND IsDeleted=0 AND VoucherDate BETWEEN @f AND @t", "Cr Note", false);

        transactions = transactions.OrderBy(t => t.Date).ThenBy(t => t.VoucherNo).ToList();

        // Running balance
        decimal balance = openingIsDebit ? opening : -opening;
        foreach (var t in transactions)
        {
            balance += t.Debit - t.Credit;
            t.Balance = Math.Abs(balance);
            t.IsDebitBalance = balance >= 0;
        }

        var totalDebit = transactions.Sum(t => t.Debit);
        var totalCredit = transactions.Sum(t => t.Credit);

        return new AccountLedgerResponse
        {
            AccountId = accountId,
            AccountName = accountName,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OpeningBalance = opening,
            OpeningIsDebit = openingIsDebit,
            Transactions = transactions,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = Math.Abs(balance),
            ClosingIsDebit = balance >= 0,
        };
    }

    public async Task<List<RecentTransaction>> GetDayBookAsync(DateTime date)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var list = new List<RecentTransaction>();
        var dateStr = date.ToString("yyyy-MM-dd");

        async Task AddTxns(string sql, string txnType)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            c.Parameters.AddWithValue("@tid", _db.TenantId);
            c.Parameters.AddWithValue("@dt", dateStr);
            using var r = await c.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new RecentTransaction
                {
                    VoucherNo = r.GetString(0),
                    Date = date,
                    AccountName = r.IsDBNull(1) ? "" : r.GetString(1),
                    Amount = r.GetDecimal(2),
                    Type = txnType
                });
        }

        await AddTxns(@"SELECT sm.VoucherNo, a.AccountName, sm.NetAmount FROM SaleMaster sm
            LEFT JOIN Accounts a ON sm.AccountId=a.Id
            WHERE sm.TenantId=@tid AND sm.IsDeleted=0 AND sm.SaleDate=@dt", "Sale");
        await AddTxns(@"SELECT pm.VoucherNo, a.AccountName, pm.NetAmount FROM PurchaseMaster pm
            LEFT JOIN Accounts a ON pm.AccountId=a.Id
            WHERE pm.TenantId=@tid AND pm.IsDeleted=0 AND pm.PurchaseDate=@dt", "Purchase");
        await AddTxns(@"SELECT rm.VoucherNo, a.AccountName, rm.Amount FROM ReceiptMaster rm
            LEFT JOIN Accounts a ON rm.AccountId=a.Id
            WHERE rm.TenantId=@tid AND rm.IsDeleted=0 AND rm.VoucherDate=@dt", "Receipt");
        await AddTxns(@"SELECT py.VoucherNo, a.AccountName, py.Amount FROM PaymentMaster py
            LEFT JOIN Accounts a ON py.AccountId=a.Id
            WHERE py.TenantId=@tid AND py.IsDeleted=0 AND py.VoucherDate=@dt", "Payment");

        return list.OrderBy(t => t.VoucherNo).ToList();
    }

    public async Task<DashboardResponse> GetDashboardDataAsync()
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        async Task<decimal> Scalar(string sql, params (string, object)[] ps)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
            var r = await c.ExecuteScalarAsync();
            return r == null || r == DBNull.Value ? 0 : Convert.ToDecimal(r);
        }

        async Task<int> ScalarInt(string sql, params (string, object)[] ps)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
            var r = await c.ExecuteScalarAsync();
            return r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
        }

        var todaySales = await Scalar("SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster WHERE TenantId=@tid AND IsDeleted=0 AND SaleDate=@dt",
            ("@tid", _db.TenantId), ("@dt", today));
        var todayPurchase = await Scalar("SELECT COALESCE(SUM(NetAmount),0) FROM PurchaseMaster WHERE TenantId=@tid AND IsDeleted=0 AND PurchaseDate=@dt",
            ("@tid", _db.TenantId), ("@dt", today));
        var pendingRecovery = await Scalar(
            "SELECT COALESCE(SUM(NetAmount),0) FROM SaleMaster WHERE TenantId=@tid AND IsDeleted=0",
            ("@tid", _db.TenantId));
        var paidRecovery = await Scalar(
            "SELECT COALESCE(SUM(Amount),0) FROM ReceiptMaster WHERE TenantId=@tid AND IsDeleted=0",
            ("@tid", _db.TenantId));
        var pendingPayment = await Scalar(
            "SELECT COALESCE(SUM(NetAmount),0) FROM PurchaseMaster WHERE TenantId=@tid AND IsDeleted=0",
            ("@tid", _db.TenantId));
        var paidPayment = await Scalar(
            "SELECT COALESCE(SUM(Amount),0) FROM PaymentMaster WHERE TenantId=@tid AND IsDeleted=0",
            ("@tid", _db.TenantId));
        var lowStock = await ScalarInt(
            "SELECT COUNT(*) FROM Stocks s JOIN Products p ON s.ProductId=p.Id WHERE s.TenantId=@tid AND s.IsDeleted=0 AND p.MinQty>0 AND s.CurrentQty<=p.MinQty",
            ("@tid", _db.TenantId));
        var expiringSoon = await ScalarInt(
            "SELECT COUNT(*) FROM Stocks WHERE TenantId=@tid AND IsDeleted=0 AND CurrentQty>0 AND ExpiryDate BETWEEN date('now') AND date('now','+90 days')",
            ("@tid", _db.TenantId));
        var expired = await ScalarInt(
            "SELECT COUNT(*) FROM Stocks WHERE TenantId=@tid AND IsDeleted=0 AND CurrentQty>0 AND ExpiryDate < date('now')",
            ("@tid", _db.TenantId));

        var recentTxns = await GetDayBookAsync(DateTime.Today);

        // Low stock items
        var lowStockItems = new List<AlertItem>();
        var lsCmd = conn.CreateCommand();
        lsCmd.CommandText = @"SELECT p.Id, p.ProductName, s.BatchNo, s.ExpiryMY, s.CurrentQty, p.MinQty
            FROM Stocks s JOIN Products p ON s.ProductId=p.Id
            WHERE s.TenantId=@tid AND s.IsDeleted=0 AND p.MinQty>0 AND s.CurrentQty<=p.MinQty
            ORDER BY s.CurrentQty LIMIT 10";
        lsCmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using (var r = await lsCmd.ExecuteReaderAsync())
            while (await r.ReadAsync())
                lowStockItems.Add(new AlertItem
                {
                    ProductId = r.GetInt32(0), ProductName = r.GetString(1), BatchNo = r.GetString(2),
                    ExpiryMY = r.IsDBNull(3) ? "" : r.GetString(3), CurrentQty = r.GetDecimal(4), MinQty = r.GetDecimal(5),
                    Status = "LOW_STOCK"
                });

        // Expiring items
        var expiringItems = new List<AlertItem>();
        var exCmd = conn.CreateCommand();
        exCmd.CommandText = @"SELECT p.Id, p.ProductName, s.BatchNo, s.ExpiryMY, s.CurrentQty, 0
            FROM Stocks s JOIN Products p ON s.ProductId=p.Id
            WHERE s.TenantId=@tid AND s.IsDeleted=0 AND s.CurrentQty>0
              AND s.ExpiryDate BETWEEN date('now') AND date('now','+90 days')
            ORDER BY s.ExpiryDate LIMIT 10";
        exCmd.Parameters.AddWithValue("@tid", _db.TenantId);
        using (var r = await exCmd.ExecuteReaderAsync())
            while (await r.ReadAsync())
                expiringItems.Add(new AlertItem
                {
                    ProductId = r.GetInt32(0), ProductName = r.GetString(1), BatchNo = r.GetString(2),
                    ExpiryMY = r.IsDBNull(3) ? "" : r.GetString(3), CurrentQty = r.GetDecimal(4),
                    Status = "EXPIRING_SOON"
                });

        return new DashboardResponse
        {
            TodaySales = todaySales,
            TodayPurchase = todayPurchase,
            PendingRecovery = Math.Max(0, pendingRecovery - paidRecovery),
            PendingPayment = Math.Max(0, pendingPayment - paidPayment),
            LowStockCount = lowStock,
            ExpiringCount = expiringSoon,
            ExpiredCount = expired,
            LowStockItems = lowStockItems,
            ExpiringItems = expiringItems,
            RecentTransactions = recentTxns,
            AsOf = DateTime.Now,
        };
    }
}
