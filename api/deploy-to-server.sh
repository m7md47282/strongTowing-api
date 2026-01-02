#!/bin/bash

# ============================================
# Deployment Pipeline: Local → IIS Server
# ============================================

# Configuration - UPDATE THESE VALUES
SERVER_HOST="66.179.188.32"  # Your IIS server IP
SERVER_USER="administrator"     # Windows username for server
SERVER_PATH="C:\\inetpub\\wwwroot\\StrongTowingAPI"  # IIS app path
# Note: Migrations run automatically on app startup (auto-migration enabled)

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Deployment Pipeline Starting${NC}"
echo -e "${GREEN}========================================${NC}"

# Step 1: Restore & Build
echo -e "\n${YELLOW}[1/3] Restoring packages...${NC}"
dotnet restore
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Restore failed!${NC}"
    exit 1
fi

echo -e "\n${YELLOW}[2/3] Building project...${NC}"
dotnet build --configuration Release --no-restore
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi

# Step 2: Publish
echo -e "\n${YELLOW}[3/3] Publishing application...${NC}"
echo -e "${YELLOW}Note: Database migrations will run automatically on app startup${NC}"
rm -rf ./publish
dotnet publish StrongTowing.API \
    --configuration Release \
    --output ./publish \
    --no-build

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Publish failed!${NC}"
    exit 1
fi

# Copy production config
cp StrongTowing.API/appsettings.Production.json ./publish/appsettings.Production.json

echo -e "${GREEN}✅ Build complete!${NC}"

# Step 5: Deploy to Server
echo -e "\n${YELLOW}Deploying to server...${NC}"
echo -e "${YELLOW}Server: ${SERVER_HOST}${NC}"
echo -e "${YELLOW}Path: ${SERVER_PATH}${NC}"

# Option A: Using SCP (if you have SSH/OpenSSH on Windows server)
if command -v scp &> /dev/null; then
    echo -e "${YELLOW}Copying files via SCP...${NC}"
    scp -r ./publish/* ${SERVER_USER}@${SERVER_HOST}:${SERVER_PATH}/
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Files copied successfully!${NC}"
    else
        echo -e "${RED}❌ SCP failed. Trying alternative method...${NC}"
    fi
fi

# Option B: Using SMB/CIFS (mount Windows share)
echo -e "\n${YELLOW}Alternative: Manual copy instructions${NC}"
echo -e "1. Mount Windows share:"
echo -e "   mkdir -p ~/iis-server"
echo -e "   mount_smbfs //${SERVER_USER}@${SERVER_HOST}/C\$ ~/iis-server"
echo -e ""
echo -e "2. Copy files:"
echo -e "   cp -r ./publish/* ~/iis-server/inetpub/wwwroot/StrongTowingAPI/"
echo -e ""
echo -e "3. Or use Windows File Sharing GUI"

# Option C: Create deployment package
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Deployment package ready!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "Location: ./publish"
echo -e ""
echo -e "Next steps:"
echo -e "1. Copy ./publish/* to ${SERVER_PATH} on ${SERVER_HOST}"
echo -e "2. Restart IIS app pool (if needed)"
echo -e "3. App will auto-migrate database and seed roles on startup"
echo -e ""
echo -e "Or use:"
echo -e "  scp -r ./publish/* ${SERVER_USER}@${SERVER_HOST}:${SERVER_PATH}/"