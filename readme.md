# LocalVideoStreaming
A minimalist .net core web app to stream mp4 files over the local network.

## Features
- Browse through directories and see only mp4 files
- Streaming using HTTP range requests
- Ability to resume video later (it keep the video current time in localstorage for future playback or in case of disconnection)
- Ability to resume a video started from another PC (storing the video time in redis)

## Hosting
You can host it directly on Windows 10 with IIS. See:
https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-2.2

Make sure to install the .NET Core Hosting Bundle.

## Configuration
Open appsettings.json and change the value of RootPathMedia to the root folder.

Install redis:
```
docker pull redis
docker run -d --restart unless-stopped -p 6379:6379 --name localVideoStreaming_redis redis
```

# Copyright and license
Code released under the MIT license.