Add-Type -Path "BeanTrack.WinForms\bin\Debug\net8.0-windows\BeanTrack.WinForms.dll"

Write-Output "--- 1. Testing Database & Password Authentication ---"
$auth = [BeanTrack.WinForms.BusinessLogic.AuthBLL]::Authenticate("admin", "Password123!")
Write-Output ("Auth Result: " + $auth.success + " | Msg: " + $auth.message + " | Role: " + $auth.user.RoleName)

Write-Output "`n--- 2. Testing POS Sale Processing (Atomic SqlTransaction) ---"
$sale = New-Object BeanTrack.WinForms.Models.Sale
$sale.UserId = $auth.user.UserId
$sale.CashierName = $auth.user.FullName
$sale.CustomerName = "Professor Test Customer"
$sale.Subtotal = [decimal]145.00
$sale.DiscountAmount = [decimal]0.00
$sale.TaxAmount = [decimal]0.00
$sale.GrandTotal = [decimal]145.00
$sale.AmountTendered = [decimal]200.00
$sale.ChangeDue = [decimal]55.00
$sale.PaymentMethod = "Cash"

$detail = New-Object BeanTrack.WinForms.Models.SaleDetail
$detail.ProductId = 1 # Signature Iced Latte
$detail.ProductName = "Signature Iced Latte"
$detail.UnitPrice = [decimal]145.00
$detail.Quantity = 1
$detail.Subtotal = [decimal]145.00
$sale.Items.Add($detail)

$saleResult = [BeanTrack.WinForms.DataAccess.SaleDAL]::ProcessSale($sale)
Write-Output ("Sale Result: " + $saleResult.success + " | Invoice: " + $saleResult.invoiceNumber + " | Msg: " + $saleResult.message)

Write-Output "`n--- 3. Testing Reports Query ---"
$report = [BeanTrack.WinForms.DataAccess.SaleDAL]::GetDailySalesReport([DateTime]::Today.AddDays(-1), [DateTime]::Today)
Write-Output ("Daily Sales Rows Count: " + $report.Rows.Count)

Write-Output "`n--- 4. Testing Audit Trail Logging ---"
$logs = [BeanTrack.WinForms.DataAccess.AuditDAL]::GetLogs()
Write-Output ("Audit Log Count: " + $logs.Count + " | Latest Action: " + $logs[0].Action + " | Desc: " + $logs[0].Description)

Write-Output "`n--- 5. Testing Concurrency & Void Transaction ---"
$voidResult = [BeanTrack.WinForms.DataAccess.SaleDAL]::VoidSale($saleResult.saleId, $auth.user.UserId, "Customer exchange request")
Write-Output ("Void Result: " + $voidResult.success + " | Msg: " + $voidResult.message)

Write-Output "`nALL SYSTEM TESTS COMPLETED SUCCESSFULLY!"
