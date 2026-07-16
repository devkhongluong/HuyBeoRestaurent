@echo off
title Huy Beo Restaurant Server
echo ================================
echo   HUY BEO RESTAURANT SERVER
echo   Dang khoi dong...
echo ================================
echo.
cd /d "d:\lap trinh web\webHuyBeo"
echo Server dang chay tai: http://localhost:5000
echo Dien thoai truy cap: http://192.168.1.92:5000
echo.
echo Nhan Ctrl+C de dung server
echo.
dotnet run --launch-profile LAN
pause
