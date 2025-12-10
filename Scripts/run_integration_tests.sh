#!/bin/bash
# =============================================================================
# OmniRAG Integration Test Runner
# =============================================================================
# This script temporarily enables ONNX integration tests, runs them, and
# restores the Skip attributes afterward.
#
# Usage: ./run_integration_tests.sh [--models-path <path>]
#
# Options:
#   --models-path    Path to ONNX models directory (default: Scripts/models)
#
# Prerequisites:
#   - ONNX models exported via: python3 export_to_onnx.py
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default models path
MODELS_PATH="${SCRIPT_DIR}/models"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --models-path)
            MODELS_PATH="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [--models-path <path>]"
            echo ""
            echo "Options:"
            echo "  --models-path    Path to ONNX models directory (default: Scripts/models)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Test file path
TEST_FILE="${PROJECT_ROOT}/OmniRAG.Tests/Infrastructure/OnnxEmbeddingServiceTests.cs"

# Required model for tests
REQUIRED_MODEL="${MODELS_PATH}/all-MiniLM-L6-v2/model.onnx"
REQUIRED_TOKENIZER="${MODELS_PATH}/all-MiniLM-L6-v2/tokenizer.json"

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}🧪 OmniRAG Integration Test Runner${NC}"
echo -e "${BLUE}============================================================${NC}"
echo ""

# =============================================================================
# Step 1: Check for ONNX models
# =============================================================================
echo -e "${YELLOW}📦 Checking for ONNX models...${NC}"

if [[ ! -f "$REQUIRED_MODEL" ]]; then
    echo -e "${RED}❌ ONNX model not found: ${REQUIRED_MODEL}${NC}"
    echo ""
    echo -e "${YELLOW}To export models, run:${NC}"
    echo "  cd ${SCRIPT_DIR}"
    echo "  python3 export_to_onnx.py"
    echo ""
    exit 1
fi

if [[ ! -f "$REQUIRED_TOKENIZER" ]]; then
    echo -e "${RED}❌ Tokenizer not found: ${REQUIRED_TOKENIZER}${NC}"
    echo ""
    echo -e "${YELLOW}To export models, run:${NC}"
    echo "  cd ${SCRIPT_DIR}"
    echo "  python3 export_to_onnx.py"
    echo ""
    exit 1
fi

echo -e "${GREEN}✅ ONNX models found${NC}"
echo "   Model: ${REQUIRED_MODEL}"
echo "   Tokenizer: ${REQUIRED_TOKENIZER}"
echo ""

# =============================================================================
# Step 2: Backup original test file
# =============================================================================
echo -e "${YELLOW}📋 Backing up test file...${NC}"
BACKUP_FILE="${TEST_FILE}.bak"
cp "$TEST_FILE" "$BACKUP_FILE"
echo -e "${GREEN}✅ Backup created: ${BACKUP_FILE}${NC}"
echo ""

# =============================================================================
# Step 3: Update models path and remove Skip attributes
# =============================================================================
echo -e "${YELLOW}🔧 Enabling integration tests...${NC}"

# Create a temporary file with modifications
TEMP_FILE="${TEST_FILE}.tmp"

# Update the TestModelsPath constant to use the actual models path
# And remove Skip attributes from the integration test methods
sed -e "s|private const string TestModelsPath = \"models\";|private const string TestModelsPath = \"${MODELS_PATH}\";|g" \
    -e 's/\[Fact(Skip = "Requires ONNX models - run '\''python export_to_onnx.py'\'' first")\]/[Fact]/g' \
    "$TEST_FILE" > "$TEMP_FILE"

mv "$TEMP_FILE" "$TEST_FILE"

echo -e "${GREEN}✅ Integration tests enabled${NC}"
echo ""

# =============================================================================
# Step 4: Run the tests
# =============================================================================
echo -e "${YELLOW}🚀 Running integration tests...${NC}"
echo ""

cd "$PROJECT_ROOT"

# Run only the integration tests
TEST_RESULT=0
dotnet test --filter "FullyQualifiedName~OnnxEmbeddingServiceIntegrationTests" --verbosity normal 2>&1 || TEST_RESULT=$?

echo ""

# =============================================================================
# Step 5: Restore original test file
# =============================================================================
echo -e "${YELLOW}🔄 Restoring original test file...${NC}"
mv "$BACKUP_FILE" "$TEST_FILE"
echo -e "${GREEN}✅ Test file restored${NC}"
echo ""

# =============================================================================
# Step 6: Report results
# =============================================================================
echo -e "${BLUE}============================================================${NC}"
if [[ $TEST_RESULT -eq 0 ]]; then
    echo -e "${GREEN}✅ Integration tests PASSED${NC}"
else
    echo -e "${RED}❌ Integration tests FAILED (exit code: ${TEST_RESULT})${NC}"
fi
echo -e "${BLUE}============================================================${NC}"

exit $TEST_RESULT
