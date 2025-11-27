# Migration Script: Database BLOB Storage to FileStore
# PowerShell script to migrate existing files from database to FileStore

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString,

    [Parameter(Mandatory=$true)]
    [string]$FileStoreApiUrl,

    [string]$LegacyTable = "LegacyDocuments",
    [int]$BatchSize = 100,
    [string]$DefaultBucket = "migrated-documents"
)

Add-Type -AssemblyName System.Data

function Get-LegacyDocuments {
    param([int]$offset)

    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()

    $query = @"
        SELECT
            DocumentId,
            FileData,
            ContentType,
            FileName,
            Channel,
            Operation,
            EntityId
        FROM $LegacyTable
        WHERE MigratedToFileStore = 0
        ORDER BY CreatedDate
        OFFSET $offset ROWS FETCH NEXT $BatchSize ROWS ONLY
"@

    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset) | Out-Null

    $connection.Close()
    return $dataset.Tables[0]
}

function Upload-ToFileStore {
    param(
        [byte[]]$fileData,
        [string]$fileName,
        [string]$contentType,
        [string]$channel,
        [string]$operation,
        [string]$entityId
    )

    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllBytes($tempFile, $fileData)

    try {
        $form = @{
            file = Get-Item -Path $tempFile
            channel = $channel
            operation = $operation
            businessEntityId = $entityId
        }

        $response = Invoke-RestMethod -Uri "$FileStoreApiUrl/buckets/$DefaultBucket/objects" `
            -Method Post `
            -Form $form

        return $response.objectId
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function Update-MigrationStatus {
    param(
        [int]$documentId,
        [string]$objectId
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()

    $query = @"
        UPDATE $LegacyTable
        SET MigratedToFileStore = 1,
            FileStoreObjectId = @ObjectId,
            MigrationDate = GETUTCDATE()
        WHERE DocumentId = @DocumentId
"@

    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $command.Parameters.AddWithValue("@ObjectId", $objectId) | Out-Null
    $command.Parameters.AddWithValue("@DocumentId", $documentId) | Out-Null
    $command.ExecuteNonQuery() | Out-Null

    $connection.Close()
}

# Main migration loop
Write-Host "Starting migration from $LegacyTable to FileStore" -ForegroundColor Green
Write-Host "API URL: $FileStoreApiUrl" -ForegroundColor Green
Write-Host "Batch Size: $BatchSize" -ForegroundColor Green
Write-Host ""

$offset = 0
$totalMigrated = 0
$totalErrors = 0

do {
    Write-Host "Fetching batch starting at offset $offset..." -ForegroundColor Cyan

    $documents = Get-LegacyDocuments -offset $offset

    if ($documents.Rows.Count -eq 0) {
        break
    }

    foreach ($doc in $documents.Rows) {
        try {
            Write-Host "  Migrating document $($doc.DocumentId) - $($doc.FileName)..." -NoNewline

            $objectId = Upload-ToFileStore `
                -fileData $doc.FileData `
                -fileName $doc.FileName `
                -contentType $doc.ContentType `
                -channel $doc.Channel `
                -operation $doc.Operation `
                -entityId $doc.EntityId

            Update-MigrationStatus -documentId $doc.DocumentId -objectId $objectId

            $totalMigrated++
            Write-Host " OK (ObjectId: $objectId)" -ForegroundColor Green
        }
        catch {
            $totalErrors++
            Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    $offset += $BatchSize

    Write-Host "Progress: $totalMigrated migrated, $totalErrors errors" -ForegroundColor Yellow
    Write-Host ""

} while ($documents.Rows.Count -eq $BatchSize)

Write-Host "Migration completed!" -ForegroundColor Green
Write-Host "Total migrated: $totalMigrated" -ForegroundColor Green
Write-Host "Total errors: $totalErrors" -ForegroundColor $(if ($totalErrors -eq 0) { "Green" } else { "Red" })
