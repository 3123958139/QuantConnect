cd /Lean/ToolBox/bin/Debug


dotnet /Lean/ToolBox/bin/Debug/QuantConnect.ToolBox.dll --app=rdg --start=19980101 --end=20240319 --symbol-count=1000 --resolution=Daily --random-seed=123456 --rename-percentage=0.0 --ipo-percentage=0.0 --splits-percentage=0.0 --dividends-percentage=0.0 --dividend-every-quarter-percentage=0.0


dotnet /Lean/ToolBox/bin/Debug/QuantConnect.ToolBox.dll --app=cug --start=19980101 --end=20240319 --symbol-count=1000 --resolution=Daily --random-seed=123456 --rename-percentage=0.0 --ipo-percentage=0.0 --splits-percentage=0.0 --dividends-percentage=0.0 --dividend-every-quarter-percentage=0.0

dotnet /home/lean/Desktop/Lean-master/Launcher/bin/Debug/QuantConnect.Lean.Launcher.dll

