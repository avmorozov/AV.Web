cd ..
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe AV.Web.sln /t:Build /p:Configuration=Release
nuget pack
nuget push AV.Web.0.1.0.nupkg -ApiKey d1a21db8-15d1-4245-90c1-ede149fdd530