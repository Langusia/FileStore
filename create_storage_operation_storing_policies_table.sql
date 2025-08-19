-- Create StorageOperationStoringPolicies table
CREATE TABLE doc.StorageOperationStoringPolicies (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    StorageOperationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    TransitionInDays INT NOT NULL,
    ExpirationInDays INT NULL,
    CONSTRAINT FK_StorageOperationStoringPolicies_StorageOperations 
        FOREIGN KEY (StorageOperationId) REFERENCES doc.StorageOperations(Id)
);

-- Create index for better performance when querying by StorageOperationId
CREATE INDEX IX_StorageOperationStoringPolicies_StorageOperationId 
    ON doc.StorageOperationStoringPolicies(StorageOperationId);

-- Insert some sample data (optional)
-- INSERT INTO doc.StorageOperationStoringPolicies (Id, StorageOperationId, Name, TransitionInDays, ExpirationInDays)
-- VALUES 
--     (NEWID(), 'your-storage-operation-id-here', 'Default Policy', 90, NULL),
--     (NEWID(), 'your-storage-operation-id-here', 'Short Term Policy', 30, 365);
