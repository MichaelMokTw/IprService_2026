@ECHO OFF
net session >nul 2>&1
IF NOT %ERRORLEVEL% EQU 0 (
   ECHO 錯誤: 請以 Administrator 權限執行.
   PAUSE
   EXIT /B 1
)

set batdir=%~dp0

ECHO 開始安裝 Richpod-IprService_H323 服務...
ECHO ---------------------------------------------------
sc create "Richpod-IprService_H323" binPath= "%batdir%..\IprService_H323.exe" DisplayName= "Richpod-IprService_H323" Start=delayed-auto
ECHO ---------------------------------------------------

ECHO 正在設定 Richpod-IprService_H323 服務名稱...
ECHO ---------------------------------------------------
sc description Richpod-IprService_H323 "Richpod-IprService_H323"
ECHO ---------------------------------------------------

ECHO 正在啟動 Richpod-IprService_H323 服務...
ECHO ---------------------------------------------------
net start "Richpod-IprService_H323"
ECHO ---------------------------------------------------

ECHO 結束
PAUSE