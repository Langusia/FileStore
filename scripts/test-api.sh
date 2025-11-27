#!/bin/bash
# API Testing Script

BASE_URL="http://localhost:5000"
BUCKET="test-bucket"

echo "=== FileStore API Test Script ==="
echo ""

# Test 1: Health Check
echo "1. Health Check..."
curl -s "${BASE_URL}/health" | jq .
echo -e "\n"

# Test 2: Upload a file
echo "2. Uploading test file..."
echo "This is a test file" > /tmp/test-file.txt

UPLOAD_RESPONSE=$(curl -s -X POST "${BASE_URL}/buckets/${BUCKET}/objects" \
  -F "file=@/tmp/test-file.txt" \
  -F "channel=test-channel" \
  -F "operation=test-operation" \
  -F "businessEntityId=test-123")

echo $UPLOAD_RESPONSE | jq .
OBJECT_ID=$(echo $UPLOAD_RESPONSE | jq -r '.objectId')
echo "Object ID: $OBJECT_ID"
echo -e "\n"

# Test 3: Get metadata
echo "3. Getting object metadata..."
curl -s -I "${BASE_URL}/buckets/${BUCKET}/objects/${OBJECT_ID}" | grep "X-"
echo -e "\n"

# Test 4: Download the file
echo "4. Downloading object..."
curl -s "${BASE_URL}/buckets/${BUCKET}/objects/${OBJECT_ID}" -o /tmp/downloaded-file.txt
cat /tmp/downloaded-file.txt
echo -e "\n"

# Test 5: List objects
echo "5. Listing objects in bucket..."
curl -s "${BASE_URL}/buckets/${BUCKET}/objects?maxKeys=10" | jq .
echo -e "\n"

# Test 6: Change tier
echo "6. Changing object tier to Cold..."
curl -s -X POST "${BASE_URL}/buckets/${BUCKET}/objects/${OBJECT_ID}/tier" \
  -H "Content-Type: application/json" \
  -d '{"tier": 1}'
echo -e "\n"

# Test 7: Delete object
echo "7. Deleting object..."
curl -s -X DELETE "${BASE_URL}/buckets/${BUCKET}/objects/${OBJECT_ID}"
echo "Deleted"
echo -e "\n"

# Cleanup
rm -f /tmp/test-file.txt /tmp/downloaded-file.txt

echo "=== Tests Complete ==="
