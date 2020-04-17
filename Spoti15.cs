using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Spoti15
{
    class Spoti15
    {
        private Exception initExcpt;

        private LogiLcd lcd;

        private Timer spotTimer;
        private Timer lcdTimer;
        private Timer refreshTimer;
        private Timer descFlipTimer;
        private Timer inputTimer;
        private Timer disableLikedSongNotificationTimer;
        private Timer hideErrorTimer;

        private uint scrollStep = 0;
        private bool descFlip = false;

        private bool showAlbum = true;
        private bool showAnimatedLines = true;
        private bool showUpNext = false;
        private AuthorizationCodeAuth auth;
        private SpotifyWebAPI api;

        private string _clientId = ""; //"";
        private string _secretId = ""; //"";
        private string refreshToken = "";
        private bool authorized = false;
        private bool cachedLikedTrack = false;
        private bool likedSongNotification = false;
        private bool unlikedSongNotification = false;
        private bool showingError = false;
        private string errorString = "";
        private FullTrack likedOrUnlikedSong;
        private PlaylistTrack upNextPlaylistTrack;
        private SimpleTrack upNextAlbumTrack;
        private FullTrack upNextAlbumTrackFull;
        private FullPlaylist cachedPlaylist;
        private FullAlbum cachedAlbum;
        private PlaybackContext cachedPlayback;

        public Spoti15()
        {
            initExcpt = null;

            InitSpot();

            lcd = new LogiLcd("Spotify");

            spotTimer = new Timer();
            spotTimer.Interval = 500;
            spotTimer.Enabled = true;
            spotTimer.Tick += OnSpotTimer;

            lcdTimer = new Timer();
            lcdTimer.Interval = 100;
            lcdTimer.Enabled = true;
            lcdTimer.Tick += OnLcdTimer;

            refreshTimer = new Timer();
            refreshTimer.Interval = 500;
            refreshTimer.Enabled = true;
            refreshTimer.Tick += OnRefreshTimer;

            descFlipTimer = new Timer();
            descFlipTimer.Interval = 2000;
            descFlipTimer.Enabled = true;
            descFlipTimer.Tick += OnDescFlip;

            inputTimer = new Timer();
            inputTimer.Interval = 1;
            inputTimer.Enabled = true;
            inputTimer.Tick += CheckInput;

            UpdateSpot();
            UpdateLcd();
        }

        private void OnDescFlip(object source, EventArgs e)
        {
            descFlip = !descFlip;
        }

        private void OnSpotTimer(object source, EventArgs e)
        {
            UpdateSpot();
        }

        private bool btn0Before = false;
        private bool btn1Before = false;
        private bool btn2Before = false;
        private bool btn3Before = false;
        private bool seeking = false;
        private bool inControlMenu = false;
        private double currentSeekPosition = 0.0f;
        private static double seekSpeed = 0.003;

        private void CheckInput(object source, EventArgs e)
        {
            seeking = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono2);

            inControlMenu = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono3);
            if(inControlMenu && !seeking)
            {
                bool btn2InCtlNow = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono2);
                if(btn2InCtlNow && !btn2Before)
                {
                    var error = api.PausePlayback();
                    if(error.HasError())
                    {
                        showingError = true;
                        errorString = error.Error.Message;
                        hideErrorTimer = new Timer();
                        hideErrorTimer.Enabled = true;
                        hideErrorTimer.Interval = 3000;
                        hideErrorTimer.Tick += OnErrorHidden;
                    }
                }
                btn2Before = btn2InCtlNow;

                bool btn1InCtlNow = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono1);
                if(btn1InCtlNow && !btn1Before)
                {
                    var error = api.SkipPlaybackToNext();
                    if (error.HasError())
                    {
                        showingError = true;
                        errorString = error.Error.Message;
                        hideErrorTimer = new Timer();
                        hideErrorTimer.Enabled = true;
                        hideErrorTimer.Interval = 3000;
                        hideErrorTimer.Tick += OnErrorHidden;
                    }
                }
                btn1Before = btn1InCtlNow;

                bool btn0InCtlNow = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono0);
                if(btn0InCtlNow && !btn0Before)
                {
                    var error = api.SkipPlaybackToPrevious();
                    if (error.HasError())
                    {
                        showingError = true;
                        errorString = error.Error.Message;
                        hideErrorTimer = new Timer();
                        hideErrorTimer.Enabled = true;
                        hideErrorTimer.Interval = 3000;
                        hideErrorTimer.Tick += OnErrorHidden;
                    }
                }
                btn0Before = btn0InCtlNow;
                return;
            }

            if (seeking && !btn2Before && cachedPlayback != null && cachedPlayback.CurrentlyPlayingType != TrackType.Ad)
            {
                // set seek position to current
                currentSeekPosition = (double)cachedPlayback.ProgressMs / cachedPlayback.Item.DurationMs;
            }

            btn2Before = seeking;

            if(seeking)
            { 
                bool btn0InSeekNow = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono0);
                if(btn0InSeekNow && !btn0Before)
                {
                    int toMs = (int)(currentSeekPosition * cachedPlayback.Item.DurationMs);
                    var error = api.SeekPlayback(toMs);
                    if (error.HasError())
                    {
                        showingError = true;
                        errorString = error.Error.Message;
                        hideErrorTimer = new Timer();
                        hideErrorTimer.Enabled = true;
                        hideErrorTimer.Interval = 3000;
                        hideErrorTimer.Tick += OnErrorHidden;
                    }
                    seeking = false;
                    btn0Before = true;
                    return;
                }
                btn0Before = btn0InSeekNow;

                if(lcd.IsButtonPressed(LogiLcd.LcdButton.Mono1))
                {
                    currentSeekPosition -= seekSpeed;
                }
                if(lcd.IsButtonPressed(LogiLcd.LcdButton.Mono3))
                {
                    currentSeekPosition += seekSpeed;
                }

                if(currentSeekPosition > 1.0)
                {
                    currentSeekPosition = 1.0;
                }
                else if(currentSeekPosition < 0.0)
                {
                    currentSeekPosition = 0.0;
                }

                return;
            }

            bool btn0Now = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono0);
            if(btn0Now && !btn0Before)
            {
                var thisItem = cachedPlayback;
                if(thisItem == null || thisItem.Item == null)
                {
                    return;
                }

                if(likedSongNotification || unlikedSongNotification)
                {
                    likedSongNotification = unlikedSongNotification = false;
                    btn0Before = btn0Now;
                    disableLikedSongNotificationTimer.Enabled = false;
                    return;
                }

                var ListedItem = new List<string>(1);

                likedOrUnlikedSong = thisItem.Item;
                ListedItem.Add(likedOrUnlikedSong.Id);
                if(cachedLikedTrack)
                {
                    api.RemoveSavedTracks(ListedItem);
                    likedSongNotification = false;
                    unlikedSongNotification = true;
                }
                else
                {
                    api.SaveTrack(likedOrUnlikedSong.Id);
                    likedSongNotification = true;
                    unlikedSongNotification = false;
                }

                disableLikedSongNotificationTimer = new Timer();
                disableLikedSongNotificationTimer.Enabled = true;
                disableLikedSongNotificationTimer.Interval = 5000;
                disableLikedSongNotificationTimer.Tick += OnLikedSongNotificationFinished;
            }

            btn0Before = btn0Now;

            bool btn1Now = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono1);
            if(btn1Now)
            {
                var playback = cachedPlayback;
                if(playback == null)
                {
                    return;
                }

                if(playback.CurrentlyPlayingType == TrackType.Ad)
                {   // doing calls later down here is bad
                    return;
                }

                if(playback.Context == null)
                {

                }
                else if(playback.Context.Type == "playlist")
                {
                    var playlist = cachedPlaylist;
                    if (playlist == null)
                    {
                        return;
                    }

                    upNextPlaylistTrack = null;
                    for (int i = 0; i < playlist.Tracks.Items.Count; i++)
                    {
                        if (playlist.Tracks.Items[i].Track.Uri == playback.Item.Uri)
                        {
                            // next track is it
                            if (i == playlist.Tracks.Items.Count - 1)
                            {
                                upNextPlaylistTrack = playlist.Tracks.Items[0];
                            }
                            else
                            {
                                upNextPlaylistTrack = playlist.Tracks.Items[i + 1];
                            }
                            break;
                        }
                    }
                    if (upNextPlaylistTrack == null)
                    {
                        return;
                    }
                }
                else if(playback.Context.Type == "album")
                {
                    upNextPlaylistTrack = null;

                    var newAlbum = cachedAlbum;
                    if(newAlbum == null)
                    {
                        return;
                    }

                    for(int i = 0; i < newAlbum.Tracks.Items.Count; i++)
                    {
                        if(playback.Item.Uri == newAlbum.Tracks.Items[i].Uri)
                        {
                            if(i == newAlbum.Tracks.Items.Count - 1)
                            {
                                upNextAlbumTrack = null;
                            }
                            else
                            {
                                upNextAlbumTrack = newAlbum.Tracks.Items[i + 1];
                            }
                        }
                    }
                }
                
            }

            showUpNext = btn1Now;
        }

        private void OnErrorHidden(object source, EventArgs e)
        {
            showingError = false;
            hideErrorTimer.Enabled = false;
        }

        private void OnLikedSongNotificationFinished(object source, EventArgs e)
        {
            likedSongNotification = unlikedSongNotification = false;
            disableLikedSongNotificationTimer.Enabled = false;
        }
        
        private void OnLcdTimer(object source, EventArgs e)
        {
            UpdateLcd();
            scrollStep += 1;
        }

        private void OnRefreshTimer(object source, EventArgs e)
        {
            //InitSpot();
        }

        public void Dispose()
        {
            lcd.Dispose();

            spotTimer.Enabled = false;
            spotTimer.Dispose();
            spotTimer = null;

            lcdTimer.Enabled = false;
            lcdTimer.Dispose();
            lcdTimer = null;

            refreshTimer.Enabled = false;
            refreshTimer.Dispose();
            refreshTimer = null;

            initExcpt = null;
            auth.Stop(0);
        }

        private void UpdateAccessToken(Token token)
        {
            api = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
            authorized = true;
            refreshToken = token.RefreshToken;
            Environment.SetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN", refreshToken);
        }

        private async void OnAuthReceived(object sender, AuthorizationCode payload)
        {
            try
            {
                auth.Stop();

                var token = await auth.ExchangeCode(payload.Code);
                UpdateAccessToken(token);
            }
            catch (Exception e)
            {
                initExcpt = e;
            }
        }

        private void Authorize()
        {
            var url = "http://localhost:4002";

            System.Diagnostics.Debug.Write("Re-authorize\r\n");
            auth?.Stop();
            auth = new AuthorizationCodeAuth(_clientId, _secretId, url, url, Scope.UserReadCurrentlyPlaying | 
                Scope.UserReadPlaybackState | Scope.UserLibraryRead | Scope.UserLibraryModify | Scope.UserModifyPlaybackState);

            if (string.IsNullOrEmpty(refreshToken))
            {
                auth.Start();
                auth.AuthReceived += OnAuthReceived;
                auth.OpenBrowser();
            }
            else
            {
                RefreshAccessToken();
            }
        }

        private async Task RefreshAccessToken()
        {
            try
            {
                var token = await auth.RefreshToken(refreshToken);
                if(token.HasError())
                {
                    refreshToken = null;
                    System.Diagnostics.Debug.Write("Bad token\r\n");
                    Authorize();
                    return;
                }
            }
            catch(Exception e)
            {
                initExcpt = e;
            }
        }

        private void InitSpot()
        {
            try
            {
                _clientId = string.IsNullOrEmpty(_clientId) ? Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID") : _clientId;
                _secretId = string.IsNullOrEmpty(_secretId) ? Environment.GetEnvironmentVariable("SPOTIFY_SECRET_ID") : _secretId;
                refreshToken = string.IsNullOrEmpty(refreshToken) ? Environment.GetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN") : refreshToken;

                System.Diagnostics.Debug.Write("InitSpot()\r\n");
                Authorize();
            }
            catch (Exception e)
            {
                initExcpt = e;
            }
        }

        public void UpdateSpot()
        {
            if(initExcpt != null)
                return;

            if (api == null)
                return;

            var retrievedPlayback = api.GetPlayback();
            if(retrievedPlayback != null)
            {
                cachedPlayback = retrievedPlayback;
                if(cachedPlayback.HasError() && cachedPlayback.Error.Message == "The access token expired" && authorized)
                {
                    authorized = false;
                    Authorize();
                }
            }

            if(cachedPlayback != null && cachedPlayback.Item != null)
            {
                var ListedItem = new List<string>(1);
                ListedItem.Add(cachedPlayback.Item.Id);
                var response = api.CheckSavedTracks(ListedItem);
                if(response.List == null)
                {
                    if (response.HasError() && response.Error.Message == "The access token expired" && authorized)
                    {
                        authorized = false;
                        Authorize();
                    }
                }
                else
                {
                    cachedLikedTrack = response.List[0];
                }

                if(cachedPlayback.Context == null)
                {

                }
                else if(cachedPlayback.Context.Type == "playlist")
                {
                    var split = cachedPlayback.Context.ExternalUrls["spotify"].Split('/');
                    var newPlaylist = api.GetPlaylist(split[4]);
                    if(newPlaylist != null && !newPlaylist.HasError())
                    {
                        cachedPlaylist = newPlaylist;
                    }
                }
                else if(cachedPlayback.Context.Type == "album")
                {
                    var newAlbum = api.GetAlbum(cachedPlayback.Item.Album.Id);
                    if(newAlbum != null && !newAlbum.HasError())
                    {
                        cachedAlbum = newAlbum;
                    }

                    if(upNextAlbumTrack != null)
                    {
                        upNextAlbumTrackFull = api.GetTrack(cachedPlayback.Item.Id);
                    }
                    else
                    {
                        upNextAlbumTrackFull = null;
                    }
                }
                
                
            }
            
        }

        private Bitmap bgBitmap = new Bitmap(LogiLcd.MonoWidth, LogiLcd.MonoHeight);
        private Font mainFont = new Font(Program.GetFontFamily("6pxbus"), 6, GraphicsUnit.Pixel);
        private Font iconFont = new Font(Program.GetFontFamily("5px2bus"), 5, GraphicsUnit.Pixel);
        private Font bigFont = new Font(Program.GetFontFamily("11px3bus"), 11, GraphicsUnit.Pixel);
        private Color bgColor = Color.Black;
        private Color fgColor = Color.White;
        private Brush bgBrush = Brushes.Black;
        private Brush fgBrush = Brushes.White;

        private void SetupGraphics(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.PageUnit = GraphicsUnit.Pixel;
            g.TextContrast = 0;

            g.Clear(bgColor);
        }

        private void DrawText(Graphics g, int line, string text, Font fnt, int offset = 0, int vertOffset = 0)
        {
            int x = offset;
            int y = (line * 6) + vertOffset;
            if (line == 0)
                y -= 1; // offset first line 3 pixels up
            TextRenderer.DrawText(g, text, fnt, new Point(x, y), fgColor, TextFormatFlags.NoPrefix);
        }

        private void DrawTextWithinBounds(Graphics g, int line, string text, Font fnt, int x, int w)
        {
            int y = (line * 6);
            if (line == 0)
                y -= 1;
            TextRenderer.DrawText(g, text, fnt, new Rectangle(x, y, w, 6), Color.White, TextFormatFlags.NoPrefix);
        }

        private void DrawTextScroll(Graphics g, int line, string text, Font fnt, bool center = true)
        {
            Size textSize = TextRenderer.MeasureText(text, fnt);

            if (textSize.Width <= LogiLcd.MonoWidth + 2)
            {
                if (center)
                {
                    int offset = (LogiLcd.MonoWidth - textSize.Width) / 2;
                    DrawText(g, line, text, fnt, offset);
                }
                else
                {
                    DrawText(g, line, text, fnt);
                }

                return;
            }

            int pxstep = 4;
            int speed = 5;
            int prewait = 5;
            int postwait = 5;

            int olen = textSize.Width - LogiLcd.MonoWidth;
            int len = pxstep * (int)((scrollStep / speed) % ((olen / pxstep) + prewait + postwait) - prewait);
            if (len < 0)
                len = 0;
            if (len > olen)
                len = olen;

            DrawText(g, line, text, fnt, -len);
        }

        private void DrawTextScroll(Graphics g, int line, string text, bool center = true)
        {
            DrawTextScroll(g, line, text, mainFont, center);
        }

        private void DrawText(Graphics g, int line, string text, int offset = 0)
        {
            DrawText(g, line, text, mainFont, offset);
        }

        private void DoRender()
        {
            lcd.MonoSetBackground(bgBitmap);
            lcd.Update();
        }

        private string GetStringFromArtists(SimpleArtist[] artists)
        {
            string returnValue = "";

            for (int i = 0; i < artists.Length; i++)
            {
                returnValue = string.Concat(returnValue, artists[i].Name);
                if (i != artists.Length - 1)
                {
                    returnValue = string.Concat(returnValue, ", ");
                }
            }

            return returnValue;
        }

        private string GetStringFromGenres(string[] genres)
        {
            string returnValue = "";

            for (int i = 0; i < genres.Length; i++)
            {
                returnValue = string.Concat(returnValue, genres[i]);
                if (i != genres.Length - 1)
                {
                    returnValue = string.Concat(returnValue, ", ");
                }
            }

            return returnValue;
        }

        private void DrawPlaybackStatus(Graphics g, bool drawTime)
        {
            var playback = cachedPlayback;

            int len = playback.Item.DurationMs;
            int pos = playback.ProgressMs;
            double perc = pos / (double)len;

            // Draw following status
            DrawText(g, 6, cachedLikedTrack ? "e" : "f", iconFont, 0, 1);

            // Draw progress bar
            g.DrawRectangle(Pens.White, 8, LogiLcd.MonoHeight - 6, (LogiLcd.MonoWidth - 24), 4);
            g.FillRectangle(Brushes.White, 8, LogiLcd.MonoHeight - 6, (int)((LogiLcd.MonoWidth - 24) * perc), 4);

            if (playback.IsPlaying)
            {
                g.FillPolygon(Brushes.White, new Point[] {
                            new Point((LogiLcd.MonoWidth - 14), LogiLcd.MonoHeight - 7),
                            new Point((LogiLcd.MonoWidth - 14), LogiLcd.MonoHeight - 1),
                            new Point((LogiLcd.MonoWidth - 9), LogiLcd.MonoHeight - 4)
                        });

                if (lineTrack > 12)
                    lineTrack = 6;
                else
                    lineTrack++;
                for (int x = lineTrack; x < LogiLcd.MonoWidth - 22; x += 8)
                    g.DrawLine(Pens.Black, new Point(x, LogiLcd.MonoHeight - 4), new Point(x + 2, LogiLcd.MonoHeight - 4));
            }
            else
            {
                g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 10), LogiLcd.MonoHeight - 6, 2, 5));
                g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 13), LogiLcd.MonoHeight - 6, 2, 5));
            }

            if (playback.ShuffleState)
            {
                DrawText(g, 6, "S", mainFont, LogiLcd.MonoWidth - 7, -1);
            }
            else if (playback.RepeatState == RepeatState.Context)
            {
                DrawText(g, 6, "h", iconFont, LogiLcd.MonoWidth - 7);
            }
            else if (playback.RepeatState == RepeatState.Track)
            {
                DrawText(g, 6, "g", iconFont, LogiLcd.MonoWidth - 7);
            }

            if(drawTime)
            {
                DrawTextScroll(g, 5, String.Format("{0}:{1:D2} / {2}:{3:D2}", pos / 60000, (pos % 60000) / 1000, len / 60000, (len % 60000) / 1000));
            }
        }

        private void DrawPlaylistStatus(Graphics g)
        {
            var playback = cachedPlayback;

            if (playback.Context == null)
            {

            }
            else if (playback.Context.Type == "album")
            {
                if (descFlip)
                {
                    DrawText(g, 0, "Playing Album");
                }
                else
                {
                    DrawTextWithinBounds(g, 0, playback.Item.Album.Name, mainFont, 0, 110);
                }
            }
            else if (playback.Context.Type == "playlist")
            {
                var playlist = cachedPlaylist;
                if (playlist != null)
                {
                    if (descFlip)
                    {
                        DrawTextWithinBounds(g, 0, playlist.Type, mainFont, 0, 110);
                    }
                    else
                    {
                        DrawTextWithinBounds(g, 0, playlist.Name, mainFont, 0, 110);
                    }
                }
            }
            else
            {
                DrawText(g, 3, "Unknown");
            }
        }

        //private Byte[] emptyBg = new Byte[LogiLcd.MonoWidth * LogiLcd.MonoHeight];
        private int lineTrack = 4;
        public void UpdateLcd()
        {
            if (initExcpt != null)
            {
                using (Graphics g = Graphics.FromImage(bgBitmap))
                {
                    SetupGraphics(g);
                    DrawText(g, 0, "Exception:");
                    DrawText(g, 1, initExcpt.GetType().ToString());
                    DrawTextScroll(g, 2, initExcpt.Message, false);
                }

                DoRender();
                return;
            }

            using (Graphics g = Graphics.FromImage(bgBitmap))
            {
                SetupGraphics(g);

                try
                {
                    if(api == null)
                    {
                        // TODO: draw spotify logo
                        g.Clear(bgColor);
                        DrawTextScroll(g, 2, "SPOTIFY");
                    }
                    else if(showingError)
                    {
                        g.Clear(bgColor);

                        DrawTextScroll(g, 1, "ERROR", bigFont);
                        DrawTextScroll(g, 3, errorString);

                        DrawPlaybackStatus(g, true);
                        DrawPlaylistStatus(g);
                    }
                    else if(likedSongNotification || unlikedSongNotification)
                    {
                        g.Clear(bgColor);

                        if (likedSongNotification)
                        {
                            DrawTextScroll(g, 1, "LIKED SONG!", bigFont);
                        }
                        else if (unlikedSongNotification)
                        {
                            DrawTextScroll(g, 1, "UNLIKED SONG!", bigFont);
                        }

                        DrawTextScroll(g, 3, likedOrUnlikedSong.Name);
                        DrawTextScroll(g, 4, GetStringFromArtists(likedOrUnlikedSong.Artists.ToArray()));
                        DrawTextScroll(g, 5, likedOrUnlikedSong.Album.Name);

                        DrawPlaybackStatus(g, false);
                    }
                    else if(seeking && cachedPlayback != null && cachedPlayback.Item != null)
                    {
                        g.Clear(bgColor);

                        DrawTextScroll(g, 1, "SEEKING");

                        var posInMs = (int)(cachedPlayback.Item.DurationMs * currentSeekPosition);
                        DrawTextScroll(g, 3, string.Format("{0:D2}:{1:D2}", posInMs / 60000, (posInMs % 60000) / 1000));

                        // Draw progress bar
                        g.DrawRectangle(Pens.White, 8, LogiLcd.MonoHeight - 16, (LogiLcd.MonoWidth - 24), 6);
                        g.FillRectangle(Brushes.White, 8, LogiLcd.MonoHeight - 16, (int)((LogiLcd.MonoWidth - 24) * currentSeekPosition), 6);

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point((LogiLcd.MonoWidth - 19), LogiLcd.MonoHeight - 7),
                            new Point((LogiLcd.MonoWidth - 19), LogiLcd.MonoHeight - 1),
                            new Point((LogiLcd.MonoWidth - 14), LogiLcd.MonoHeight - 4)
                        });

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point(60, LogiLcd.MonoHeight - 7),
                            new Point(60, LogiLcd.MonoHeight - 1),
                            new Point(55, LogiLcd.MonoHeight - 4)
                        });

                        DrawText(g, 6, "OK", iconFont, 8);
                        DrawPlaylistStatus(g);
                    }
                    else if(inControlMenu && cachedPlayback != null)
                    {
                        g.Clear(bgColor);

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point((LogiLcd.MonoWidth - 100), LogiLcd.MonoHeight - 7),
                            new Point((LogiLcd.MonoWidth - 100), LogiLcd.MonoHeight - 1),
                            new Point((LogiLcd.MonoWidth - 95), LogiLcd.MonoHeight - 4)
                        });

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point((LogiLcd.MonoWidth - 106), LogiLcd.MonoHeight - 7),
                            new Point((LogiLcd.MonoWidth - 106), LogiLcd.MonoHeight - 1),
                            new Point((LogiLcd.MonoWidth - 101), LogiLcd.MonoHeight - 4)
                        });

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point(17, LogiLcd.MonoHeight - 7),
                            new Point(17, LogiLcd.MonoHeight - 1),
                            new Point(12, LogiLcd.MonoHeight - 4)
                        });

                        g.FillPolygon(Brushes.White, new Point[] {
                            new Point(23, LogiLcd.MonoHeight - 7),
                            new Point(23, LogiLcd.MonoHeight - 1),
                            new Point(18, LogiLcd.MonoHeight - 4)
                        });

                        if (cachedPlayback.IsPlaying)
                        {
                            g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 58), LogiLcd.MonoHeight - 6, 2, 5));
                            g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 61), LogiLcd.MonoHeight - 6, 2, 5));
                        }
                        else
                        {
                            g.FillPolygon(Brushes.White, new Point[] {
                                new Point((LogiLcd.MonoWidth - 64), LogiLcd.MonoHeight - 7),
                                new Point((LogiLcd.MonoWidth - 64), LogiLcd.MonoHeight - 1),
                                new Point((LogiLcd.MonoWidth - 59), LogiLcd.MonoHeight - 4)
                            });
                        }

                        DrawTextScroll(g, 1, "UP NEXT", bigFont);

                        if(cachedPlayback.Context == null)
                        {

                        }
                        else if (cachedPlayback.Context.Type == "playlist" && upNextPlaylistTrack != null)
                        {
                            DrawTextScroll(g, 3, upNextPlaylistTrack.Track.Name);
                            DrawTextScroll(g, 4, GetStringFromArtists(upNextPlaylistTrack.Track.Artists.ToArray()));
                            DrawTextScroll(g, 5, upNextPlaylistTrack.Track.Album.Name);

                            DrawText(g, 0, string.Format("e {0}", upNextPlaylistTrack.Track.Popularity), iconFont, 5, 5);
                        }
                        else if (cachedPlayback.Context.Type == "album" && upNextAlbumTrack != null)
                        {
                            DrawTextScroll(g, 3, upNextAlbumTrack.Name);
                            DrawTextScroll(g, 4, string.Format("Track {0}", upNextAlbumTrack.TrackNumber));

                            if (upNextAlbumTrackFull != null)
                            {
                                DrawText(g, 0, string.Format("e {0}", upNextAlbumTrackFull.Popularity), iconFont, 5, 5);
                            }
                        }
                        else
                        {
                            DrawTextScroll(g, 3, "Unknown");
                        }
                    }
                    else if(showUpNext)
                    {
                        g.Clear(bgColor);

                        if(cachedPlayback.Context == null)
                        {

                        }
                        else if(cachedPlayback.Context.Type == "playlist")
                        {
                            DrawTextScroll(g, 1, "PLAYLIST", bigFont);
                            DrawTextScroll(g, 3, cachedPlaylist.Name);
                            DrawTextScroll(g, 4, cachedPlaylist.Description);

                            DrawText(g, 0, string.Format("e {0}", cachedPlaylist.Followers.Total), iconFont, 5, 5);

                            DrawPlaybackStatus(g, true);
                        }
                        else if(cachedPlayback.Context.Type == "album")
                        {
                            DrawTextScroll(g, 1, "ALBUM", bigFont);
                            DrawTextScroll(g, 3, cachedPlayback.Item.Album.Name);
                            DrawTextScroll(g, 4, GetStringFromArtists(cachedPlayback.Item.Artists.ToArray()));
                            DrawTextScroll(g, 5, string.Format("Released {0}", cachedPlayback.Item.Album.ReleaseDate));

                            if(cachedAlbum != null)
                            {
                                string genres = GetStringFromGenres(cachedAlbum.Genres.ToArray());
                                if(genres == "")
                                {
                                    DrawText(g, 0, string.Format("e {0}", cachedAlbum.Popularity), iconFont, 5, 5);
                                }
                                else
                                {
                                    DrawTextScroll(g, 6, string.Format("{0} - {1} followers", GetStringFromGenres(cachedAlbum.Genres.ToArray()), cachedAlbum.Popularity));
                                }
                            }

                            DrawPlaybackStatus(g, true);
                        }
                    }
                    else
                    {

                        var playback = cachedPlayback;
                        if (playback == null || playback.Item == null)
                        {
                            if (playback == null)
                            {
                                g.Clear(bgColor);
                                DrawTextScroll(g, 1, "ERROR");
                                DrawTextScroll(g, 2, "SPOTIFY PLAYBACK NOT DETECTED");
                                DoRender();
                                return;
                            }
                            else if (playback.CurrentlyPlayingType == TrackType.Ad)
                            {
                                g.Clear(bgColor);
                                DrawTextScroll(g, 2, "Advertisement");
                                DoRender();
                                return;
                            }
                            else if (playback.HasError())
                            {
                                g.Clear(bgColor);
                                DrawTextScroll(g, 1, "ERROR");
                                DrawTextScroll(g, 2, playback.Error.Message);
                            }
                            else
                            {
                                return;
                            }
                        }
                        

                        // Draw number of followers
                        DrawText(g, 5, string.Format("e {0}", cachedPlayback.Item.Popularity), iconFont);

                        DrawTextScroll(g, 2, GetStringFromArtists(playback.Item.Artists.ToArray()));
                        DrawTextScroll(g, 1, playback.Item.Name);
                        DrawTextScroll(g, 3, playback.Item.Album.Name);

                        DrawPlaybackStatus(g, true);
                        DrawPlaylistStatus(g);
                    }

                    string currentTime = DateTime.Now.ToString("h:mm:ss tt");
                    Size textSize = TextRenderer.MeasureText(currentTime, mainFont);
                    DrawText(g, 0, currentTime, LogiLcd.MonoWidth - textSize.Width);
                }
                catch (Exception e)
                {
                    g.Clear(bgColor);
                    var split = e.StackTrace.Split('\\');
                    DrawTextScroll(g, 1, string.Format("Exception: {0}", e.GetType()));
                    DrawTextScroll(g, 2, split[split.Length - 1]);
                    //DrawTextScroll(g, 1, "No track information available", false);
                }
            }

            DoRender();
        }
    }
}
