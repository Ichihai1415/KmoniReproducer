@echo off
echo �y���Ӂz�摜�t�@�C��������t�H���_�ɃR�s�[���Ď��s���Ă��������Bffmpeg�̃p�X���ʂ��Ă���K�v������܂��B
timeout /t 2 /nobreak >nul
set /p f="fps����͂��Ă�������: "
ffmpeg -framerate %f% -i %%04d.png -vcodec libx264 -pix_fmt yuv420p -r %f% _output_%f%.mp4
echo �������܂����B
set /p delOK="d�Ɠ��͂���Ƃ���bat�t�@�C�����폜���܂��B"
IF %delOK%==d (
del %0
)