@echo off
echo Starting Freelance Music API...
echo.

echo API will be available at: https://localhost:7000
echo Swagger UI will be available at: https://localhost:7000/swagger
echo.

REM Start the API server
dotnet run

REM Keep the window open after the API stops
pause
