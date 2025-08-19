# StorageOperationStoringPolicy API Documentation

This API provides full CRUD operations for managing storage operation storing policies in the Credo File Storage system.

## Overview

Storage Operation Storing Policies define lifecycle rules for files stored in specific storage operations. These policies control:
- **TransitionInDays**: How many days before files are moved to cold storage
- **ExpirationInDays**: How many days before files are permanently deleted (optional)

## Base URL
```
https://localhost:7001/api/StorageOperationStoringPolicy
```

## Endpoints

### 1. Get All Policies
**GET** `/api/StorageOperationStoringPolicy`

Retrieves all storage operation storing policies.

**Response:**
```json
[
  {
    "id": "12345678-1234-1234-1234-123456789012",
    "storageOperationId": "87654321-4321-4321-4321-210987654321",
    "name": "Default Policy",
    "transitionInDays": 90,
    "expirationInDays": null,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

### 2. Get Policy by ID
**GET** `/api/StorageOperationStoringPolicy/{id}`

Retrieves a specific storage operation storing policy by its ID.

**Parameters:**
- `id` (Guid): The unique identifier of the policy

**Response:**
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "storageOperationId": "87654321-4321-4321-4321-210987654321",
  "name": "Default Policy",
  "transitionInDays": 90,
  "expirationInDays": null,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### 3. Get Policy by StorageOperationId
**GET** `/api/StorageOperationStoringPolicy/by-storage-operation/{storageOperationId}`

Retrieves a storage operation storing policy by the storage operation ID.

**Parameters:**
- `storageOperationId` (Guid): The unique identifier of the storage operation

**Response:** Same as Get Policy by ID

### 4. Create Policy
**POST** `/api/StorageOperationStoringPolicy`

Creates a new storage operation storing policy.

**Request Body:**
```json
{
  "storageOperationId": "87654321-4321-4321-4321-210987654321",
  "name": "Default Policy",
  "transitionInDays": 90,
  "expirationInDays": null
}
```

**Validation Rules:**
- `storageOperationId`: Required, must exist in StorageOperations table
- `name`: Required, max 255 characters
- `transitionInDays`: Required, must be greater than 0
- `expirationInDays`: Optional, must be greater than 0 if provided

**Response:** 201 Created with the created policy

### 5. Update Policy
**PUT** `/api/StorageOperationStoringPolicy/{id}`

Updates an existing storage operation storing policy.

**Parameters:**
- `id` (Guid): The unique identifier of the policy to update

**Request Body:**
```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "storageOperationId": "87654321-4321-4321-4321-210987654321",
  "name": "Updated Policy",
  "transitionInDays": 60,
  "expirationInDays": 180
}
```

**Validation Rules:** Same as Create Policy

**Response:** 200 OK with the updated policy

### 6. Delete Policy
**DELETE** `/api/StorageOperationStoringPolicy/{id}`

Deletes a storage operation storing policy.

**Parameters:**
- `id` (Guid): The unique identifier of the policy to delete

**Response:** 204 No Content

## Error Responses

### 400 Bad Request
```json
{
  "error": "Storage operation not found"
}
```

### 404 Not Found
```json
{
  "error": "Storage operation storing policy not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Failed to create storage operation storing policy",
  "details": "Detailed error message"
}
```

## Business Rules

1. **One Policy Per Storage Operation**: Only one policy can exist per storage operation
2. **Storage Operation Validation**: Policies can only be created for existing storage operations
3. **Unique Names**: Policy names should be descriptive and unique within the system
4. **Transition vs Expiration**: 
   - TransitionInDays is required and must be greater than 0
   - ExpirationInDays is optional but must be greater than 0 if provided
   - ExpirationInDays should typically be greater than TransitionInDays

## Usage Examples

### Creating a Default Policy
```bash
curl -X POST "https://localhost:7001/api/StorageOperationStoringPolicy" \
  -H "Content-Type: application/json" \
  -d '{
    "storageOperationId": "87654321-4321-4321-4321-210987654321",
    "name": "Default Policy",
    "transitionInDays": 90,
    "expirationInDays": null
  }'
```

### Creating a Short-Term Policy
```bash
curl -X POST "https://localhost:7001/api/StorageOperationStoringPolicy" \
  -H "Content-Type: application/json" \
  -d '{
    "storageOperationId": "87654321-4321-4321-4321-210987654321",
    "name": "Short Term Policy",
    "transitionInDays": 30,
    "expirationInDays": 365
  }'
```

## Integration with File Storage

When files are uploaded without a specific storing policy, the system will:
1. Look up the default policy for the storage operation
2. Apply the lifecycle rules (transition and expiration) to the uploaded files
3. If no policy exists, fall back to the hardcoded default (90 days transition)

This provides a flexible, database-driven approach to lifecycle management while maintaining backward compatibility.
