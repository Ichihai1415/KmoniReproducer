@echo off
echo 【注意】画像ファイルがあるフォルダにコピーして実行してください。ffmpegのパスが通っている必要があります。
timeout /t 2 /nobreak >nul
set /p f="fpsを入力してください: "
ffmpeg -framerate %f% -i %%04d.png -vcodec libx264 -pix_fmt yuv420p -r %f% _output_%f%.mp4
echo 完了しました。
set /p delOK="dと入力するとこのbatファイルを削除します。"
IF %delOK%==d (
del %0
)