﻿
using System;
using System.IO;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using Microsoft.Xna.Framework.Input;

namespace FF8
{
    internal static class Module_movie_test
    {
        private const int STATE_INIT = 0;
        private const int STATE_CLEAR = 1;
        private const int STATE_PLAYING = 2;
        private const int STATE_PAUSED = 3;
        private const int STATE_FINISHED = 4;
        private const int STATE_RETURN = 5;

        private static readonly string[] movieDirs = {
            MakiExtended.GetUnixFullPath(Path.Combine(Memory.FF8DIR, "../movies")), //this folder has most movies
            MakiExtended.GetUnixFullPath(Path.Combine(Memory.FF8DIR, "movies"))}; //this folder has rest of movies
        private static List<string> _movies = new List<string>();
        /// <summary>
        /// Movie file list
        /// </summary>
        public static List<string> Movies
        {
            get
            {
                if (_movies.Count == 0)
                {
                    foreach (string s in movieDirs)
                    {
                        _movies.AddRange(Directory.GetFiles(s, "*.avi"));
                    }
                }
                return _movies;
            }
        }

        private static Bitmap Frame { get; set; } = null;
        private static Ffcc Ffccvideo { get; set; } = null;
        private static Texture2D LastFrame { get; set; } = null;
        private static Ffcc Ffccaudio { get; set; } = null;
        public static int ReturnState { get; set; } = Memory.MODULE_MAINMENU_DEBUG;
        /// <summary>
        /// Index in movie file list
        /// </summary>
        public static int Index { get; set; } = 0;
        private static int FPS { get; set; } = 0;
        private static int FrameRenderingDelay { get; set; } = 0;
        private static int MsElapsed { get; set; } = 0;
        public static int MovieState { get; set; } = STATE_INIT;

        internal static void Update()
        {
            if (Input.Button(Buttons.Okay) || Input.Button(Buttons.Cancel) || Input.Button(Keys.Space))
            {
                Input.ResetInputLimit();
                //init_debugger_Audio.StopAudio();
                //Memory.module = Memory.MODULE_MAINMENU_DEBUG;
                MovieState = STATE_RETURN;
            }
            switch (MovieState)
            {
                case STATE_INIT:
                    MovieState++;
                    InitMovie();
                    break;
                case STATE_CLEAR:
                    MovieState++;
                    ClearScreen();
                    break;
                case STATE_PLAYING:
                    PlayingDraw();
                    break;
                case STATE_PAUSED:
                    break;
                case STATE_FINISHED:
                    MovieState++;
                    FinishedDraw();
                    break;
                case STATE_RETURN:
                default:
                    Memory.module = ReturnState;
                    MovieState = STATE_INIT;
                    LastFrame = null;
                    Frame = null;
                    ReturnState = Memory.MODULE_MAINMENU_DEBUG;
                    break;
            }
        }


        // The flush packet is a non-null packet with size 0 and data null

        private static void InitMovie()
        {

            //vfr.Open(Path.Combine(movieDirs[0] , "disc02_25h.avi"));
            //vfr.Open(Path.Combine(movieDirs[0], "disc00_30h.avi"));

            Ffccaudio = new Ffcc(@"c:\eyes_on_me.wav", AVMediaType.AVMEDIA_TYPE_AUDIO, Ffcc.FfccMode.PROCESS_ALL);

            //Ffccaudio = new Ffcc(Movies[Index], AVMediaType.AVMEDIA_TYPE_AUDIO, Ffcc.FfccMode.PROCESS_ALL);
            Ffccvideo = new Ffcc(Movies[Index], AVMediaType.AVMEDIA_TYPE_VIDEO, Ffcc.FfccMode.STATE_MACH);
            FPS = Ffccvideo.FPS;
            try
            {
                FrameRenderingDelay = (1000 / FPS) / 2;
            }
            catch (DivideByZeroException e)
            {
                TextWriter errorWriter = Console.Error;
                errorWriter.WriteLine(e.Message);
                errorWriter.WriteLine("Can not calc FPS, possibly FFMPEG dlls are missing or an error has occured");
                MovieState = STATE_RETURN;
            }

        }

        internal static void Draw()
        {
            //switch (movieState)
            //{
            //case STATE_INIT:
            //    break;
            //case STATE_CLEAR:
            //    ClearScreen();
            //    movieState++;
            //    break;
            //case STATE_PLAYING:
            //    PlayingDraw();
            //    break;
            //case STATE_PAUSED:
            //    break;
            //case STATE_FINISHED:
            //    FinishedDraw();
            //    break;
            //default:
            //Memory.module = Memory.MODULE_MAINMENU_DEBUG;
            //break;

            //}
        }
        private static void ClearScreen()
        {
            Memory.spriteBatch.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
        }
        private static void FinishedDraw()
        {
            ClearScreen();
            Memory.SpriteBatchStartStencil();
            if (LastFrame != null)
                Memory.spriteBatch.Draw(LastFrame, new Microsoft.Xna.Framework.Rectangle(0, 0, Memory.graphics.PreferredBackBufferWidth, Memory.graphics.PreferredBackBufferHeight), Microsoft.Xna.Framework.Color.White);
            Memory.SpriteBatchEnd();
            //movieState = STATE_INIT;
            //Memory.module = Memory.MODULE_BATTLE_DEBUG;
        }
        //private static Bitmap lastframe = null;
        //private static Bitmap Frame { get => frame; set { lastframe = frame; frame = value; } }
        private static void PlayingDraw()
        {
            Texture2D frameTex = LastFrame;
            if (LastFrame != null && MsElapsed < FrameRenderingDelay)
            {
                MsElapsed += Memory.gameTime.ElapsedGameTime.Milliseconds;
                //redraw previous frame or flickering happens.
            }
            else
            {
                MsElapsed = 0;

                int ret = Ffccvideo.GetFrame();
                if (ret < 0)
                {
                    MovieState = STATE_FINISHED;
                    return;
                }
                frameTex = Ffccvideo.FrameToTexture2D();
            }

            //draw frame;
            Memory.SpriteBatchStartStencil();
            Memory.spriteBatch.Draw(frameTex, new Microsoft.Xna.Framework.Rectangle(0, 0, Memory.graphics.PreferredBackBufferWidth, Memory.graphics.PreferredBackBufferHeight), Microsoft.Xna.Framework.Color.White);
            Memory.SpriteBatchEnd();
            //backup previous frame. use if new frame unavailble
            LastFrame = frameTex;

        }
    }
}