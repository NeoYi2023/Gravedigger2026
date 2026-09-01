@echo off
REM Unity batch pipeline for MonsterModel_08 (ZombieFemale2).
REM Set UNITY to your Unity 2021.3 Editor path if not on PATH.
setlocal
set "PROJECT=%~dp0..\Gravedigger2026"
if not defined UNITY set "UNITY=F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe"
if not exist "%UNITY%" (
  echo Unity not found at %UNITY%
  echo Set UNITY env var to Unity.exe and retry.
  exit /b 1
)
echo Repair MonsterModel_08...
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Gravedigger2026.Editor.Art.CharacterCreatorExportRepair.RepairMonsterModel08Batch -logFile "%TEMP%\mm08_repair.log"
echo Wire Die2...
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Gravedigger2026.Editor.Art.MonsterDie2AnimatorWirer.WireAllBatchExecute -logFile "%TEMP%\mm08_die2.log"
echo Assemble monster prefabs...
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Gravedigger2026.Editor.Art.MonsterModelPrefabAssembler.AssembleAllBatch -logFile "%TEMP%\mm08_assemble.log"
echo Generate Defend catalog...
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Gravedigger2026.Editor.Defend.DefendAssetBuilder.GenerateAll -logFile "%TEMP%\mm08_catalog.log"
echo Done. Check logs in %TEMP%\mm08_*.log
