@echo off
echo Starting Freelance Music API...
echo.

echo API will be available at: http://localhost:5168
echo Swagger UI will be available at: http://localhost:5168/swagger
echo.

REM Start the API server
dotnet run

REM Keep the window open after the API stops
pause
