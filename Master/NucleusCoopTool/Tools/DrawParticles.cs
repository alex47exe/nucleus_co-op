﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nucleus.Coop.Tools
{
    public class DrawParticles
    {
        private System.Threading.Timer particlesTimer;
        private Control senderControl;
        private Control handlerControl;

        private void particlesTimer_Tick(object state)
        {
            if (!handlerControl.Visible || handlerControl.IsDisposed || senderControl.IsDisposed)
            {
                particlesTimer.Dispose();
                particlesTimer = null;
                return;
            }

            senderControl.Invalidate();
        }

        private RectangleF[] particles;
        private  int[] alpha = new int[12];

        public void Draw(object sender, Control control, PaintEventArgs e, int particlesMax, int refreshRate, int[] color)
        {
            Random rand = new Random();

            if(particles == null)
            {
                particles = new RectangleF[particlesMax];
            }
       
            if (particlesTimer == null)
            {
                senderControl = sender as Control;
                handlerControl = control;

                if (control == null)
                {
                    handlerControl = senderControl;
                }

                particlesTimer = new System.Threading.Timer(particlesTimer_Tick, null, 0, refreshRate);

                for (int i = 0; i < particles.Count(); i++)
                {
                    int randMultX = rand.Next(-senderControl.Width / 2, senderControl.Width / 2);
                    int randMultY = rand.Next(-senderControl.Height / 2, senderControl.Height / 2);

                    int randX = rand.Next(0, senderControl.Width + randMultX);
                    int randY = rand.Next(0, senderControl.Height + randMultY);
                    int randS = rand.Next(3, 5);

                    particles[i] = new RectangleF(randX, randY, randS, randS);
                    
                    int randA = rand.Next(100, 255);
                    alpha[i] = randA;
                }
            }

            for (int i = 0; i < particles.Count(); i++)
            {
                particles[i].Width -= 0.2f;
                particles[i].Height -= 0.2f;

                if (particles[i].X < senderControl.Width / 2)
                {
                    particles[i].X -= 0.2f;
                }
                else
                {
                    particles[i].X += 0.2f;
                }

                particles[i].Y -= 0.2f;

                bool minSize = particles[i].Width < 0.2F;

                if (minSize)
                {
                    int randMultX = rand.Next(-senderControl.Width / 2 , senderControl.Width / 2);
                    int randMultY = rand.Next(-senderControl.Height / 2, senderControl.Height / 2);

                    int randX = rand.Next(0, senderControl.Width  + randMultX);
                    int randY = rand.Next(0, senderControl.Height + randMultY);
                    int randS = rand.Next(3, 5);

                    while(particles.Any(p => (new RectangleF(randX - 30, randY - 30, randS + 60, randS + 60).Contains(p))) || 
                          particles.Any(p => p != particles[i] && p.Width == randS))
                    {
                         randMultX = rand.Next(-senderControl.Width / 3, senderControl.Width / 3);
                         randMultY = rand.Next(-senderControl.Height / 3, senderControl.Height / 3);

                         randX = rand.Next(0, senderControl.Width + randMultX);
                         randY = rand.Next(0, senderControl.Height + randMultY);
                         randS -= 1;
                    }

                    particles[i] = new RectangleF(randX, randY, randS, randS);
                }

                SolidBrush particlesBrush = new SolidBrush(Color.FromArgb(alpha[i], color[0], color[1], color[2]));

                if (particles[i].Width > 1)
                {
                    e.Graphics.FillEllipse(particlesBrush, particles[i]);
                }

                particlesBrush.Dispose();
            }
        }

    }
}