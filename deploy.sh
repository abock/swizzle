#!/bin/bash -ex

rid=linux-x64
config=Release

host=W56B1HhBEq66IOfG3qCSv
app_path=web/swizzle/app

rm -rf _deploy
dotnet publish -r "$rid" -c "$config" -o _deploy
mv _deploy/Swizzle.Web _deploy/swizzle
chmod +x _deploy/swizzle
tar -c -f swizzle.app.tar -C _deploy .
ssh $host "rm -rf '$app_path' && mkdir -p '$app_path'"
scp swizzle.app.tar "$host:$app_path"
rm -f swizzle.app.tar
ssh $host "cd '$app_path' && tar xf swizzle.app.tar && rm swizzle.app.tar"
ssh $host "sudo -S -- bash -c \"cp '$app_path/swizzle.service' /etc/systemd/system/ && systemctl enable swizzle.service && systemctl restart swizzle.service\""