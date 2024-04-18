cd /Lean/ToolBox/bin/Debug


dotnet /Lean/ToolBox/bin/Debug/QuantConnect.ToolBox.dll --app=rdg --start=19980101 --end=20240319 --symbol-count=1000 --resolution=Daily --random-seed=123456 --rename-percentage=0.0 --ipo-percentage=0.0 --splits-percentage=0.0 --dividends-percentage=0.0 --dividend-every-quarter-percentage=0.0


dotnet /Lean/ToolBox/bin/Debug/QuantConnect.ToolBox.dll --app=cug --start=19980101 --end=20240319 --symbol-count=1000 --resolution=Daily --random-seed=123456 --rename-percentage=0.0 --ipo-percentage=0.0 --splits-percentage=0.0 --dividends-percentage=0.0 --dividend-every-quarter-percentage=0.0

dotnet /home/lean/Desktop/Lean-master/Launcher/bin/Debug/QuantConnect.Lean.Launcher.dll

1. 将所有字段显示全

docker ps -a --no-trunc

2. 安装runlike

pip install runlike

3.  查看容器创建命令

runlike 容器名称

docker run --name=vigilant_ellis --hostname=4c99ccec542a --mac-address=02:42:ac:11:00:02 --workdir=/Lean/Launcher/bin/Debug --expose=8888 --restart=no --label='devcontainer.local_folder=/home/lean/Desktop/Lean-master' --label='devcontainer.config_file=/home/lean/Desktop/Lean-master/.devcontainer/devcontainer.json' --runtime=runc vsc-lean-master-35c3b1d93de065c578b60725fb517370e22eda6cf841192e5a6b1da99c5186f1 -c 'echo Container started
trap "exit 0" 15

exec "$@"
while sleep 1 & wait $!; do :; done' -
