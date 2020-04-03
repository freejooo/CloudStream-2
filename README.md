# CloudStream 2

**DOWNLOAD:**
https://github.com/LagradOst/CloudStream-2/releases

CloudStream 2 is the successor to https://github.com/LagradOst/CloudStream, and focuses more on appearance and usability. It is a Xamarin.Forms project, but I have no working UWP build at the moment.   

**FEATURES:**
+ **AdFree**, No ads whatsoever
+ No tracking/analytics
+ Bookmark any movie or show
+ Download and stream movies, tv-shows and anime
+ Watch history that can easily be toggled and removed
+ Instant search, get results fast
+ IMDb and MAL integration
+ Recommendations for every movie
+ Trailer from IMDb
+ Movie and Episode description
+ Subtitles directly from www.opensubtitles.org 
+ Title sharing, share any movie via a link.
+ Chromecast
+ Top 100 movies 
+ YouTube Download
+ InApp video player using https://github.com/videolan/libvlcsharp

**REQUIREMENTS:**
+ On Android, you must have VLC or any other video player that can play .m3u8 files installed. 
+ On Windows, VLC is recommended, but other video players can be used. Note that *ONLY VLC* can autoselect subtitles on Windows, on other video players, and on Android, you have to do that manually.

**CANT PLAY VIDEO?**
+ Make sure that you have installed VLC, or any other video player if you are running Windows.
+ If you can't play any movie, make sure to link the .m3u8 filetype to a video player. On Android you have to go to file manager and open the mirrorlist.m3u8 file that CloudStream 2 generated as a video file. You can do this on Windows by creating a .txt file and renaming it to example.m3u8, then right click and open with VLC. 

**SCREENSHOTS:**
<p align="center">
    <img src="https://cdn.discordapp.com/attachments/542070959067103232/695591072969261056/Screenshot_20200403_131009_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text">  
        <img src="https://cdn.discordapp.com/attachments/542070959067103232/695591071870353418/Screenshot_20200403_131014_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text">  
<img src="https://cdn.discordapp.com/attachments/542070959067103232/695591073887813682/Screenshot_20200403_130959_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text">   
    <img src="https://cdn.discordapp.com/attachments/542070959067103232/695591073581629440/Screenshot_20200403_131004_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text"> 
<img src="https://cdn.discordapp.com/attachments/542070959067103232/695591075070607390/Screenshot_20200403_130854_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text">
    <img src="https://cdn.discordapp.com/attachments/542070959067103232/695591076609916998/Screenshot_20200403_130800_com.CloudStreamForms.CloudStreamForms.jpg" width="200" title="hover text">      
    <img src="https://cdn.discordapp.com/attachments/542070959067103232/695594924912934922/Screenshot_20200403_130821_com.CloudStreamForms.CloudStreamForms.png" width="200" title="hover text">
<img src="https://cdn.discordapp.com/attachments/542070959067103232/695598765184385056/Screenshot_20200403_130821_com.CloudStreamForms.CloudStreamForms.png" width="200" title="hover text">
</p>

***How it works:***

This app dosen't use a p2p connection or any private servers hosted by me, **IT IS NOT A BITTORRENT**. It takes all the links from established streaming sites by downloading the sites and extracting the useful information.

CloudStream and CloudStream 2 works in diffrent ways. CloudStream gets the links and info from the link site directly. CloudStream 2 will first search IMDb and then crossreference with the link sites and MAL to get the links. Because of this the initial loading time is a bit longer than CloudStream. Most of the relevant data is cached so the loading time is shorter when loading a title again.  

***Sites used:***

https://www.imdb.com/ (Seach, rating, trailer, recommendations and descriptions)

https://myanimelist.net/ (Only used to crossreference anime)

https://fmovies.to/ (Movie and Tv-Show HD links)

https://gomostream.com/ (Movie and Tv-Show HD links)

https://movies123.pro/ (Movie and Tv-Show mirror links)

https://1movietv.com/ (Movie and Tv-Show links)

https://yesmoviess.to/ (Movie and Tv-Show links)

https://www.freefullmovies.zone (Movie Links)

http://watchserieshd.tv (slow Tv-Show links)

https://www9.gogoanime.io/ (Anime HD links)

https://animeflix.io/ (Anime HD links)

https://www.opensubtitles.org (Subtitles)

https://js.do/code (Title sharing)
