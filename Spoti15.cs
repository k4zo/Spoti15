using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
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

        private uint scrollStep = 0;
        private bool descFlip = false;

        private bool showAlbum = true;
        private bool showAnimatedLines = true;
        private AuthorizationCodeAuth auth;
        private SpotifyWebAPI api;

        private string _clientId = ""; //"";
        private string _secretId = ""; //"";
        private string refreshToken = "";
        private bool authorized = true;

        public Spoti15()
        {
            initExcpt = null;

            InitSpot();

            lcd = new LogiLcd("Spotify");

            spotTimer = new Timer();
            spotTimer.Interval = 1000;
            spotTimer.Enabled = true;
            spotTimer.Tick += OnSpotTimer;

            lcdTimer = new Timer();
            lcdTimer.Interval = 100;
            lcdTimer.Enabled = true;
            lcdTimer.Tick += OnLcdTimer;

            refreshTimer = new Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Enabled = true;
            refreshTimer.Tick += OnRefreshTimer;

            descFlipTimer = new Timer();
            descFlipTimer.Interval = 2000;
            descFlipTimer.Enabled = true;
            descFlipTimer.Tick += OnDescFlip;

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
        private bool btn2Before = false;
        private bool btn3Before = false;
        private void OnLcdTimer(object source, EventArgs e)
        {
            /*
            bool btn0Now = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono0);
            if (btn0Now && !btn0Before)
                InitSpot();
            btn0Before = btn0Now;
            */
            UpdateLcd();
            scrollStep += 1;

            // toggle between "ARTIST - ALBUM" and "ALBUM" on line 1
            /*
            bool btn3Now = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono3);
            if (btn3Now && !btn3Before)
                showAlbum = !showAlbum;
            btn3Before = btn3Now;

            // toggle animated lines within progress bar
            bool btn2Now = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono2);
            if (btn2Now && !btn2Before)
                showAnimatedLines = !showAnimatedLines;
            btn2Before = btn2Now;
            */
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
            auth = new AuthorizationCodeAuth(_clientId, _secretId, url, url, Scope.UserReadCurrentlyPlaying | Scope.UserReadPlaybackState);

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
        }

        private Bitmap bgBitmap = new Bitmap(LogiLcd.MonoWidth, LogiLcd.MonoHeight);
        private Font mainFont = new Font(Program.GetFontFamily("5pxbus"), 9, GraphicsUnit.Pixel);
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

        private void DrawText(Graphics g, int line, string text, Font fnt, int offset = 0)
        {
            int x = offset;
            int y = line * 10;
            if (line == 0)
                y -= 1; // offset first line 3 pixels up
            TextRenderer.DrawText(g, text, fnt, new Point(x, y), fgColor, TextFormatFlags.NoPrefix);
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
                    var playback = api.GetPlayback();
                    int len = playback.Item.DurationMs;
                    int pos = playback.ProgressMs;
                    double perc = pos / (double)len;

                    String lineZero = "";
                    var artists = playback.Item.Artists.ToArray();
                    for(int i = 0; i < artists.Length; i++)
                    {
                        lineZero = string.Concat(lineZero, artists[i].Name);
                        if(i != artists.Length - 1)
                        {
                            lineZero = string.Concat(lineZero, ", ");
                        }
                    }
                    DrawTextScroll(g, 0, lineZero);
                    DrawTextScroll(g, 1, playback.Item.Name);
                    DrawText(g, 2, String.Format("{0}:{1:D2} / {2}:{3:D2}", pos / 60000, (pos % 60000) / 1000, len / 60000, (len % 60000) / 1000),
                        LogiLcd.MonoWidth - 50);
                    //DrawTextScroll(g, 2, String.Format("{0}:{1:D2} / {2}:{3:D2}", pos / 60000, (pos % 60000) / 1000, len / 60000, (len % 60000) / 1000));

                    // Draw progress bar
                    g.DrawRectangle(Pens.White, 3, 23, (LogiLcd.MonoWidth - 75), 4);
                    g.FillRectangle(Brushes.White, 3, 23, (int)((LogiLcd.MonoWidth - 75) * perc), 4);

                    if (playback.IsPlaying)
                    {
                        g.FillPolygon(Brushes.White, new Point[] { new Point((LogiLcd.MonoWidth - 69), 30), new Point((LogiLcd.MonoWidth - 69), 20), new Point((LogiLcd.MonoWidth - 64), 25) });

                        if (lineTrack > 8)
                            lineTrack = 4;
                        else
                            lineTrack++;
                        for (int x = lineTrack; x < LogiLcd.MonoWidth - 80; x += 6)
                            g.DrawLine(Pens.Black, new Point(x, 25), new Point(x + 2, 25));
                    }
                    else
                    {
                        g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 69), 22, 2, 7));
                        g.FillRectangle(Brushes.White, new Rectangle((LogiLcd.MonoWidth - 66), 22, 2, 7));
                    }

                    if (playback.Context.Type == "album")
                    {
                        if(descFlip)
                        {
                            DrawText(g, 3, "Album");
                        }
                        else
                        {
                            DrawText(g, 3, playback.Item.Album.Name);
                        }
                    }
                    else if (playback.Context.Type == "playlist")
                    {
                        var split = playback.Context.ExternalUrls["spotify"].Split('/');
                        var playlist = api.GetPlaylist(split[4]);

                        if(descFlip)
                        {
                            DrawText(g, 3, "Playlist");
                        }
                        else
                        {
                            DrawText(g, 3, playlist.Name);
                        }
                    }
                    else
                    {
                        DrawText(g, 3, "Unknown");
                    }
                }
                catch (NullReferenceException)
                {
                    g.Clear(bgColor);
                    DrawTextScroll(g, 1, "No track information available", false);
                }
            }

            DoRender();
        }
    }
}
