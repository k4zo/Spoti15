# Spoti15
### A Spotify Applet for the Logitech G15/G510S

---

I've updated this to fix a couple issues I noticed with it, everything below is straight from the original readme.md

---
 The original developer has abandoned the project so I've kept this applet updated to work with the latest version of [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) and updated it with new features.
 
## Features
1. Three display modes
2. Seeking and playback control (Spotify Premium members only)
3. Follow/Unfollow buttons
4. Text scrolling
5. Sleek display
6. Support for the latest version Spotify 

## Installation
1\. Download the Latest release of Spoti15 from [Here.](https://github.com/eezstreet/Spoti15/releases)

2\. Create a new applet in the Spotify Developer Dashboard. Save the Client ID and Secret ID.

3\. In the Applet page of the Spotify Developer Dashboard, under 'Edit Settings' enter the following URI to the 'Redirect URIs' whitelist: `http://localhost:4002`. Then click 'Add' and finally 'Save'.

4\. Run Spoti15. Use the Client ID and Secret ID in the web browser window that pops up.

5\. (OPTIONAL): Save the Client ID and Secret ID to environment variables (SPOTIFY_CLIENT_ID and SPOTIFY_SECRET_ID) to save them across sessions.

6\. Ensure that the application is selected in your Logitech Gaming Software

7\. Ensure that Spotify is Running in the background.

8\. Done!

## Main Display
![Main display](https://i.imgur.com/359JN6p.png)


### Controls

Pressing the left button (button 0) at any time will either LIKE or UNLIKE the currently playing song.

![Liked display](https://i.imgur.com/DfqLbRy.png)

Pressing and holding the left-middle button (button 1) while in the main display will show information about the currently playing playlist, album, or artist.

![Playlist Information](https://i.imgur.com/7r2hntK.png)

![Artist Information](https://i.imgur.com/8p9AgLf.png)

![Album Information](https://i.imgur.com/lmsVIOx.png)

## Seek Display
![Seek display](https://i.imgur.com/oBVqnxA.png)

Pressing and holding the right-middle button (button 2) while in the main display will bring up the Seek Display.

### Controls
Note that the Seek Display will only function if you have a Spotify Premium membership.

Pressing the left-middle button (button 1) will seek left, and pressing the right button (button 3) will seek right. Press the left button (button 0) to go to that section of the song.

## Up Next Display
![Up Next Display](https://i.imgur.com/QIOX18F.png)

Pressing and holding the right button (button 3) while in the main display will bring up the Up Next Display.

### Controls
Note that the Up Next Display will only function if you have a Spotify Premium membership.

Pressing the left button (button 0) will go to the previous song. Pressing the left-middle button will go to the next song. Pressing the right-middle button will pause/play the current track.


### *Changelog:*
```
v2.0.0 [April 12 2020]
 + Updated SpotifyAPI-NET, total rewrite of the software
 
v1.0.0.16 [April 28 2018]
 + Updated SpotifyAPI-NET to 2.18.1
 + Support for 1.0.77.338.g758ebd78

v1.0.0.15 [MARCH 13 2017]
 + Updated  SpotifyAPI-NET DLL's to Latest Version
 + Support for latest version of Spotify (Tested on 1.0.50.41368.gbd68dbef)

v1.0.0.14 [OCTOBER 08 2016]
  + '&' now displays properly instead of displaying as '_'
  + Button 2 now toggles animated lines in progress bar
  + Button 3 now toggles displaying Album name on first line
  + Updated font to native 11px for better character spacing
  + Stylistic spacing improvements

v1.0.0.13 [SEPTEMBER 28 2016]
  + Updated to work with latest version of Spotify
  + Updated SpotifyAPI-NET dll's to latest versions
  + Reduced refresh timer from 30s to 5s to autodetect Spotify.exe
  
v1.0.0.12 [OCTOBER 07 2014]
  + Fixed Obtaining OAuth key from SpotifyWebHelper.exe
```
