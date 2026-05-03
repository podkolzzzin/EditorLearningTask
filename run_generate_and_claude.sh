#!/bin/bash
# Script to generate SQL file and run Claude to open it


SCRIPT_DIR="$(dirname "$0")"
EXE_PATH="$SCRIPT_DIR/EditorLearningTask/bin/Debug/net10.0/EditorLearningTask"
TARGET_FILE="$SCRIPT_DIR/EditorLearningTask/bin/Debug/net10.0/output.sql"
echo "SQL file generated: $TARGET_FILE"

# Step 1: Generate SQL file with 1000 lines (change as needed)
"$EXE_PATH" generate 10000000
if [ $? -ne 0 ]; then
  echo "Failed to generate SQL file."
  exit 1
fi

echo "SQL file generated: $TARGET_FILE"

# Step 2: Run C# editor to open the file
"$EXE_PATH" "$TARGET_FILE"



