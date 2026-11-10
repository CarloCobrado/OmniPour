$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "omnipour_mssql.sql"
if (-not (Test-Path $sqlFile)) {
    $sqlFile = Join-Path $scriptDir "beantrack_mssql.sql"
}
$sqlContent = [System.IO.File]::ReadAllText($sqlFile)

$batches = [System.Text.RegularExpressions.Regex]::Split($sqlContent, "^\s*GO\s*$", [System.Text.RegularExpressions.RegexOptions]::Multiline)

$connStr = "Server=(localdb)\MSSQLLocalDB;Integrated Security=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Output "Connected to (localdb)\MSSQLLocalDB."

try {
    foreach ($batch in $batches) {
        $trimmed = $batch.Trim()
        if ($trimmed.Length -gt 0) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $trimmed
            $cmd.ExecuteNonQuery() | Out-Null
        }
    }
    Write-Output "DATABASE SETUP COMPLETED SUCCESSFULLY! OmniPourDB is ready."
}
catch {
    Write-Error ("Error executing batch: " + $_.Exception.Message)
}
finally {
    $conn.Close()
}
