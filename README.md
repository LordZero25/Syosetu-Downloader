# Syosetu-Downloader
Download chapters from Syosetsu ni Narou

**Requirements**
- .Net Framework 4.0

**Features:**
- Support links with the pattern "http://*.syosetu.com/xxxxxxx"
- Downloaded chapters are on .exe folder/[novel name]
- Option to specify a chapter range (i.e. if you want to only download say chapters 3-10)
- Download mutiple series
- Clicking the progress bar will cancel the download job
- Clicking the novel title will open its download folder
- Custom filename (see format.ini for details)
- Chapter list generation (nxxxxxx.htm)

**Notes:**
- If no chapter range is specified it will download everything
- If "from" is blank it will start from chapter 1
- If "to" is blank it will end on the latest/last chapter
- No volume support
- "chapter 1" is "http://*.syosetu.com/xxxxxxx/1"
- Replaces illegal characters on filename with "□"
- No chapter list generation for wayback machine

**[Releases Here](https://github.com/LordZero25/Syosetu-Downloader/releases/)**
