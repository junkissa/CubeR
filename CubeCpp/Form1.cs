using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.IO;
using System.Drawing.Imaging;
//using System.Random;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using opengl;

namespace opengl
{
    public partial class Form1 : Form
    {
        bool loaded = false;
        int N = 3;/*размерность*/

        int w = 16;
        int spacing = 2;
        int side;
        float hw;

        int viewAngle = 80;
        int zNear = 1;
        int zFar = 500;

        double x1 = 0;
        double x2 = 0;
        double y1 = 0;
        double y2 = 0;
        bool rotating = false;
        enum typeRotating { X, Y, NUL };
        typeRotating rotatingAxis = typeRotating.NUL;
        bool changedx = false;
		enum Gran {White, Yellow, Blue, Green, Pink, Orange};
		Gran[] U_white;
		Gran[] D_yellow;
		Gran[] F_blue;
		Gran[] B_green;
		Gran[] L_pink;
		Gran[] R_orange;
        string fileDebug = @"debug.txt";

        Vector3 sr;
        Vector3 er;
        bool click = false;
        DateTime click_start;
        DateTime click_end;

        List<angleXYZ> angles = new List<angleXYZ>();
        int[] positions;


        bool disableRotateAngleX = false;
        bool disableRotateAngleY = false;

        float[][][] edges;
        float[][] rotate_matrices;

        float[] T;
        float[] T2;
        float[] RR = new float[16];
        float[] G_R;

        public enum Axis { X, Y, Z };
        Dictionary<Axis, Plane[]> intersect_planes = new Dictionary<Axis, Plane[]>();

        OpenTK.Graphics.TextPrinter printer = new OpenTK.Graphics.TextPrinter(OpenTK.Graphics.TextQuality.High);

        Matrix4 G_modelview = new Matrix4();

        float[] axis_x = new float[4] { 1, 0, 0, 1 };
        float[] axis_y = new float[4] { 0, 1, 0, 1 };
        float[] axis_z = new float[4] { 0, 0, 1, 1 };

        ViewPoint vp = new ViewPoint();

        uint G_init_steps = 0;
        int prev_two_div;
        int prev_two_mod;

        int texture_id;
#region Old


		public Form1()
        {
            InitializeComponent();
          //  this.glControl1.Click += new EventHandler(mouseClick);
            this.glControl1.MouseDown += new MouseEventHandler(mouseDown);
            this.glControl1.MouseMove += new MouseEventHandler(mouseMove);
            this.glControl1.MouseUp += new MouseEventHandler(mouseUp);

            G_R = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };

            float offset = (w * N + (N - 1) * spacing) / 2;

            T = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                -offset, -offset, -offset, 1
            };

            T2 = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                +offset, +offset, +offset, 1
            };

            int n = (int)Math.Pow((double)N, 3);
            edges = new float[n][][];
            positions = new int[n];
            rotate_matrices = new float[n][];

            for (int i = 0; i < n; i++)
            {
                angleXYZ angle = new angleXYZ();
                angles.Add(angle);
                positions[i] = i;

                float[][] vectors = new float[8][] {
                    new float[4] { 0, 0, 0, 1 },
                    new float[4] { 0, 0, w, 1 },
                    new float[4] { 0, w, 0, 1 },
                    new float[4] { 0, w, w, 1 },
                    new float[4] { w, 0, 0, 1 },
                    new float[4] { w, 0, w, 1 },
                    new float[4] { w, w, 0, 1 },
                    new float[4] { w, w, w, 1 },
                };
                edges[i] = vectors;

                rotate_matrices[i] = new float[] {
                    1, 0, 0, 0,
                    0, 1, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                };

                List<int> data = getOffsets(i);
                int offset_x = data[0];
                int offset_z = data[1];
                int offset_y = data[2];

                for (int j = 0; j < edges[i].Length; j++)
                {
                    edges[i][j][0] += offset_x * (w + spacing);
                    edges[i][j][1] += offset_y * (w + spacing);
                    edges[i][j][2] += offset_z * (w + spacing);
                }
            }
            side = N * w + (N - 1) * spacing;
            hw = this.w / 2;

            Vector3 p1 = new Vector3(0, side, side);//<----
            Vector3 p2 = new Vector3(side, 0, side);
            Vector3 p3 = new Vector3(side, side, side);//<----
            Vector3 p4 = new Vector3(side, 0, 0);
            Vector3 p5 = new Vector3(0, 0, 0);
            Vector3 p6 = new Vector3(0, side, 0);//<----

            Vector3 p7 = new Vector3(0, 0, side);
            Vector3 p8 = new Vector3(side, side, 0);

            intersect_planes[Axis.X] = new Plane[2] {
                new Plane(p2, p3, p8),//кубики 2, 5, 8, 11, 14, 17, 23, 20, 26 Ось X
                new Plane(p1, p7, p5)//кубики 0, 3, 6, 9, 12, 15, 18, 21, 24 Ось X
            };

            intersect_planes[Axis.Y] = new Plane[2] {
                new Plane(p1, p3, p8),//кубики 18, 19, 20, 21, 22, 23, 24, 25, 26 Ось Y
                new Plane(p2, p4, p5)//кубики 0, 1, 2, 3, 4, 5, 6, 7, 8 Ось Y
            };

            intersect_planes[Axis.Z] = new Plane[2] {
                new Plane(p1, p3, p2), //кубики 6, 7, 8, 15, 16, 17, 24, 25, 26 Ось Z
                new Plane(p4, p5, p6)//кубики 0, 1, 2, 9, 10, 11, 18, 19, 20 Ось Z
            };

			U_white = new Gran[9];
			D_yellow = new Gran[9];
			F_blue	 = new Gran[9];
			B_green	 = new Gran[9];
			L_pink	 = new Gran[9];
			R_orange = new Gran[9];

			for (int i = 0; i < 9; ++i)
			{
				U_white[i] = Gran.White;
				D_yellow[i] = Gran.Yellow;
				F_blue	[i] = Gran.Blue;
				B_green	[i] = Gran.Green;
				L_pink	[i] = Gran.Pink;
				R_orange[i] = Gran.Orange;
			}


            File.Delete(fileDebug);
        }

        static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            Bitmap bmp = new Bitmap(filename);
            BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            bmp.UnlockBits(bmp_data);

            // We haven't uploaded mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // On newer video cards, we can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			
            return id;
        }

        

        private void glControl1_Load(object sender, EventArgs e)
        {
            loaded = true;

			GL.ClearColor(Color.PaleGreen);

            GL.Enable(EnableCap.DepthTest);
            //MessageBox.Show((Math.Atan(1) * 180 / Math.PI).ToString());
            SetupViewport();
            texture_id = LoadTexture("tree.bmp");
            GL.BindTexture(TextureTarget.Texture2D, texture_id);
            GL.Enable(EnableCap.Texture2D);
        }

        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!loaded)
                return;
            if (e.KeyCode == Keys.Z)
            {
				L();
				//EasingTimer.rotate(0, rotatePart, angles, 1, EasingTimer.rotateAxisType.X);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.Q)
            {
				L_();
				//EasingTimer.rotate(0, rotatePart, angles, -1, EasingTimer.rotateAxisType.X);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.X)
            {
				R_();
				//EasingTimer.rotate(2, rotatePart, angles, 1, EasingTimer.rotateAxisType.X);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.W)
            {
				R();
				//EasingTimer.rotate(2, rotatePart, angles, -1, EasingTimer.rotateAxisType.X);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.T)
            {
				U();
				//EasingTimer.rotate(2, rotatePart, angles, -1, EasingTimer.rotateAxisType.Y);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.U)
            {
				U_();
				//EasingTimer.rotate(2, rotatePart, angles, 1, EasingTimer.rotateAxisType.Y);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.F)
            {
				D_();
				//EasingTimer.rotate(0, rotatePart, angles, -1, EasingTimer.rotateAxisType.Y);
				//glControl1.Invalidate();
            }
            else if (e.KeyCode == Keys.H)
            {
				D();
				//EasingTimer.rotate(0, rotatePart, angles, 1, EasingTimer.rotateAxisType.Y);
				//glControl1.Invalidate();
            }
        }

        public void check_solved()
        {
            Dictionary<int, int> pos_to_cube_number = new Dictionary<int, int>(N * N * N);
            for (int i = 0; i < N * N * N; ++i)
            {
                pos_to_cube_number[positions[i]] = i;
            }

            float[] matrix;
            float[] imatrix;
            float[] point;

            Color pane_color;
            Color color;

            float step = this.w + this.spacing;

            float x = 0;
            float y = 0;
            float z = 0;

            int N2 = N * N;
            bool error = false;
            int pos;
            int n;
            int step1, step2;

            Action work = delegate
            {
                for (int i = 0; i < 2; i++)
                {
                    y = side * i;
                    x = hw;
                    z = hw;

                    pos = i * N * N * (N - 1);
                    n = pos_to_cube_number[pos];

                    matrix = rotate_matrices[n];
                    imatrix = invert(matrix);

                    point = new float[4] { x, y, z, 1 };

                    my_multiplyMatrices(ref point, imatrix);
                    pane_color = getColor(point);

                    pos++;

                    for (int j = 1; j < N2; j++)
                    {
                        n = pos_to_cube_number[pos];

                        matrix = rotate_matrices[n];
                        imatrix = invert(matrix);

                        step1 = Math.DivRem(j, N, out step2);
                        x = hw + step1 * step;
                        z = hw + step2 * step;

                        point = new float[4] { x, y, z, 1 };
                        my_multiplyMatrices(ref point, imatrix);
                        color = getColor(point);

                        if (pane_color != color)
                        {
                            error = true;
                            return;
                        }

                        pos++;
                    }
                }
            };
            work();

            Action work2 = delegate
            {
                for (int i = 0; i < 2; i++)
                {
                    x = side * i;
                    y = hw;
                    z = hw;

                    pos = i * (N - 1);
                    n = pos_to_cube_number[pos];

                    matrix = rotate_matrices[n];
                    imatrix = invert(matrix);

                    point = new float[4] { x, y, z, 1 };

                    my_multiplyMatrices(ref point, imatrix);
                    pane_color = getColor(point);

                    pos += N;

                    for (int j = 1; j < N2; j++)
                    {
                        n = pos_to_cube_number[pos];

                        matrix = rotate_matrices[n];
                        imatrix = invert(matrix);

                        step1 = Math.DivRem(j, N, out step2);
                        y = hw + step1 * step;
                        z = hw + step2 * step;

                        point = new float[4] { x, y, z, 1 };
                        my_multiplyMatrices(ref point, imatrix);
                        color = getColor(point);

                        if (pane_color != color)
                        {
                            error = true;
                            return;
                        }

                        pos += N;
                    }
                }
            };
            if (!error)
            {
                work2();
            }

            Action work3 = delegate
            {
                for (int i = 0; i < 2; i++)
                {
                    x = hw;
                    y = hw;
                    z = side * i;

                    pos = i * N * (N - 1);
                    n = pos_to_cube_number[pos];

                    matrix = rotate_matrices[n];
                    imatrix = invert(matrix);

                    point = new float[4] { x, y, z, 1 };

                    my_multiplyMatrices(ref point, imatrix);
                    pane_color = getColor(point);

                    pos++;

                    for (int j = 1; j < N2; j++)
                    {
                        n = pos_to_cube_number[pos];

                        matrix = rotate_matrices[n];
                        imatrix = invert(matrix);

                        step1 = Math.DivRem(j, N, out step2);
                        x = hw + step1 * step;
                        y = hw + step2 * step;

                        point = new float[4] { x, y, z, 1 };
                        my_multiplyMatrices(ref point, imatrix);
                        color = getColor(point);

                        if (pane_color != color)
                        {
                            error = true;
                            return;
                        }

                        pos = (int)(Math.Floor((double)j / N) * N * N) + j;
                    }
                }
            };

            if (!error)
            {
                work3();
            }

            if (!error)
            {
                MessageBox.Show("Кубик собран!");
            }
        }

        public Color getColor(float x, float y, float z)
        {
            float[] point = new float[3] { x, y, z };
            return getColor(point);
        }

        public Color getColor(float[] point)
        {
            float eps = 0.00001f;
            if (Math.Abs(point[0]) < eps)
            {
                return Color.Blue;
            }
            else if (Math.Abs(point[0] - side) < eps)
            {
                return Color.Black;
            }

            if (Math.Abs(point[1]) < eps)
            {
                return Color.Red;
            }
            else if (Math.Abs(point[1] - side) < eps)
            {
                return Color.Yellow;
            }

            if (Math.Abs(point[2]) < eps)
            {
                return Color.White;
            }
            else if (Math.Abs(point[2] - side) < eps)
            {
                return Color.Green;
            }

            return Color.Green;
        }

        public float[] invert(float[] matrix)
        {
            Matrix4 Matrix = new Matrix4(
                    matrix[0], matrix[1], matrix[2], matrix[3],
                    matrix[4], matrix[5], matrix[6], matrix[7],
                    matrix[8], matrix[9], matrix[10], matrix[11],
                    matrix[12], matrix[13], matrix[14], matrix[15]
                );
            Matrix4 iMatrix = Matrix4.Invert(Matrix);
            float[] ret_matrix = new float[16];
            ret_matrix[0] = iMatrix.M11;
            ret_matrix[1] = iMatrix.M12;
            ret_matrix[2] = iMatrix.M13;
            ret_matrix[3] = iMatrix.M14;
            ret_matrix[4] = iMatrix.M21;
            ret_matrix[5] = iMatrix.M22;
            ret_matrix[6] = iMatrix.M23;
            ret_matrix[7] = iMatrix.M24;
            ret_matrix[8] = iMatrix.M31;
            ret_matrix[9] = iMatrix.M32;
            ret_matrix[10] = iMatrix.M33;
            ret_matrix[11] = iMatrix.M34;
            ret_matrix[12] = iMatrix.M41;
            ret_matrix[13] = iMatrix.M42;
            ret_matrix[14] = iMatrix.M43;
            ret_matrix[15] = iMatrix.M44;

            return ret_matrix;
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            SetupViewport();
            glControl1.Invalidate();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            Render();
        }

        private void mouseDown(object sender, EventArgs e)
        {
            System.Windows.Forms.MouseEventArgs me = (e as System.Windows.Forms.MouseEventArgs);
            this.x1 = me.X;
            this.y1 = me.Y;
            this.x2 = me.X;
            this.y2 = me.Y;
            this.rotating = true;
            click = true;
        }

        private void mouseMove(object sender, EventArgs e)
        {
            if (!this.rotating)
            {
                return;
            }
            click = false;

            System.Windows.Forms.MouseEventArgs me = (e as System.Windows.Forms.MouseEventArgs);

            int w = glControl1.Width;
            int h = glControl1.Height;

            float dx = -(float)(me.X - this.x1);
            float dy = -(float)(me.Y - this.y1);

            if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1 && rotatingAxis == typeRotating.NUL)
            {
                return;
            }

            rotatingAxis = typeRotating.X;

            float drotation = (dx / w) * 180;
            float drotation2 = (dy / h) * 180;

            vp.angle_view_beta -= drotation2;
            drotation *= vp.orintation_y;
            if (changedx)
            {
                drotation *= -1;
            }
            if ((vp.angle_view_beta > 90 && vp.angle_view_beta < 135) ||
            (vp.angle_view_beta > 270 && vp.angle_view_beta < 315))
            {
                drotation *= -1;
            }

            vp.angle_view_alfa += drotation;


            G_modelview = Matrix4.LookAt(vp.viewX, vp.viewY, vp.viewZ, 0, 0, 0, 0, vp.orintation_y, 0);

            this.x1 = me.X;
            this.y1 = me.Y;

            
            glControl1.Invalidate();
        }

        private void mouseUp(object sender, EventArgs e)
        {
            this.rotating = false;
            this.rotatingAxis = typeRotating.NUL;

            System.Windows.Forms.MouseEventArgs me = (e as System.Windows.Forms.MouseEventArgs);
            if (Math.Abs(me.X - this.x2) < 5 && Math.Abs(me.Y - this.y2) < 5)
            {
                click = true;
                /*this.x2 = me.X;
                this.y2 = me.Y;*/
            }


           
        }

        private void rotatePart(object sender, ElapsedEventArgs e)
        {
            EasingTimer t = sender as EasingTimer;
            t.Stop();

            long time = e.SignalTime.Ticks - t.begin_time.Ticks;
            time /= 10000;
            if (time > t.duration)
            {
                time = t.duration;
            }

            //MessageBox.Show("dir = " + t.dir);

            int currentAngle = (int)(90 * t.dir * time / t.duration);

            int i, j, k, l, m;
            int[] new_pos = new int[N * N];
            Dictionary<int, int> pos_to_cube_number = new Dictionary<int, int>(N * N * N);
            for (i = 0; i < N * N * N; i++)
            {
                pos_to_cube_number[positions[i]] = i;
            }

            //MessageBox.Show(t.rotateAxis.ToString());

            if (t.rotateAxis == EasingTimer.rotateAxisType.Y)
            {
                i = t.rotateN * N * N;
                for (j = 1; j <= N * N; j++)
                {
                    k = pos_to_cube_number[i];//k - номер кубика
                    angles[k].Y = currentAngle;
                    i++;
                }
            }
            else if (t.rotateAxis == EasingTimer.rotateAxisType.X)
            {
                i = t.rotateN;//0, 3, 6, 9, 12, 15, 18, 21, 24,                
                for (j = 1; j <= N * N; j++)
                {
                    k = pos_to_cube_number[i];//k - номер кубика
                    angles[k].X = currentAngle;
                    i += N;
                }
            }
            else if (t.rotateAxis == EasingTimer.rotateAxisType.Z)
            {
                i = t.rotateN * N;
                for (j = 0; j < N; j++)
                {
                    l = i;//0, 1, 2, 9, 10, 11, 18, 19, 20
                    for (k = 0; k < N; k++)
                    {
                        //debug("Z: l = " + l);
                        m = pos_to_cube_number[l];//m - номер кубика
                        angles[m].Z = currentAngle;
                        l++;
                    }
                    i += N * N;
                }
            }

            if (time < t.duration)
            {
                t.Start();
            }
            else
            {
                rotatePartFast(t.rotateAxis, t.rotateN, t.dir);
                t.run = false;
            }
            glControl1.Invalidate();
        }

        private void SetupViewport()
        {
            int w = glControl1.Width;
            int h = glControl1.Height;
            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
            GL.Translate(w / 2, h / 2, 0);

            G_modelview = Matrix4.LookAt(vp.viewX, vp.viewY, vp.viewZ, 0, 0, 0, 0, vp.orintation_y, 0);

            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView((float)(viewAngle * Math.PI / 180), (float)(1), zNear, zFar);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref p);
        }

        private void Render()
        {
            if (!loaded) // Play nice
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref G_modelview);

			float offset = (w * N + (N - 1) * spacing) / 2;//середина куба
			GL.Translate(
			   -offset,
			   -offset,
			   -offset);

            int n3 = N * N * N;
			for (int i = 0; i < n3; i++)
			{
				cube(i);
			}
			GL.PopMatrix();
            glControl1.SwapBuffers();
        }
        

      
        void my_multiplyMatrices(ref float[] a, float[] b)
        {
            float[] a_copy = new float[16];
            int i, j, k, rows;
            float s;
            for (i = 0; i < a.Length; i++)
            {
                a_copy[i] = a[i];
            }

            rows = a.Length / 4;
            for (k = 0; k < rows; k++)
            {
                for (i = 0; i < 4; i++)
                {
                    s = 0;
                    for (j = 0; j < 4; j++)
                    {
                        s += a_copy[k * 4 + j] * b[j * 4 + i];
                    }
                    a[k * 4 + i] = s;
                }
            }
        }


        float[] setRotateMatrix(float angle, Axis axis)
        {
            float[] RR = new float[16];
            float cos = (float)Math.Cos((angle * Math.PI) / 180);
            float sin = (float)Math.Sin((angle * Math.PI) / 180);

            if (axis == Axis.X)
            {
                RR[0] = 1; RR[1] = 0; RR[2] = 0; RR[3] = 0;
                RR[4] = 0; RR[5] = cos; RR[6] = -sin; RR[7] = 0;
                RR[8] = 0; RR[9] = sin; RR[10] = cos; RR[11] = 0;
                RR[12] = 0; RR[13] = 0; RR[14] = 0; RR[15] = 1;
            }
            else if (axis == Axis.Y)
            {
                RR[0] = cos; RR[1] = 0; RR[2] = sin; RR[3] = 0;
                RR[4] = 0; RR[5] = 1; RR[6] = 0; RR[7] = 0;
                RR[8] = -sin; RR[9] = 0; RR[10] = cos; RR[11] = 0;
                RR[12] = 0; RR[13] = 0; RR[14] = 0; RR[15] = 1;
            }
            else if (axis == Axis.Z)
            {
                RR[0] = cos; RR[1] = -sin; RR[2] = 0; RR[3] = 0;
                RR[4] = sin; RR[5] = cos; RR[6] = 0; RR[7] = 0;
                RR[8] = 0; RR[9] = 0; RR[10] = 1; RR[11] = 0;
                RR[12] = 0; RR[13] = 0; RR[14] = 0; RR[15] = 1;
            }
            return RR;
        }

        void my_rotateCubeEdgeArroundSelf(ref float[] a, float angle, Axis axis, int position)
        {
            float offset_x, offset_y, offset_z;

            offset_x = edges[position][0][0];
            offset_z = edges[position][0][1];
            offset_y = edges[position][0][2];

            MessageBox.Show("offset_x = " + offset_x + ", offset_y = " + offset_y + ", offset_z = " + offset_z);

            T[12] = -offset_x;
            T[13] = -offset_y;
            T[14] = -offset_z;

            T2[12] = offset_x;
            T2[13] = offset_y;
            T2[14] = offset_z;

            my_multiplyMatrices(ref a, T);

            float x = axis_x[0];
            float y = axis_x[1];
            float z = axis_x[2];

            float cos_a = (float)(x / Math.Sqrt(x * x + z * z));
            float a_x = (float)(Math.Acos(cos_a) * 180 / Math.PI);
            float cos_b = (float)(Math.Sqrt(x * x + z * z) / Math.Sqrt(x * x + y * y + z * z));
            float b = (float)(Math.Acos(cos_b) * 180 / Math.PI);

        }

        void rotateAroundAxis(ref float[] a, float angle, Axis axis)
        {
            float[] RR;
            RR = setRotateMatrix(angle, axis);
            my_multiplyMatrices(ref a, RR);
        }

        void rotate_cube(int k, float angle, Axis axis, bool self = true)
        {
            List<int> data = getOffsets(k);
            int off_x = data[0];
            int off_z = data[1];
            int off_y = data[2];

            float offset_x = (w + spacing) * off_x;
            //MessageBox.Show("offset_x = " + offset_x);
            float offset_y = (w + spacing) * off_y;
            float offset_z = (w + spacing) * off_z;

            float w2 = w / 2;

            float[] T = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };

            float center_offset = (N * w + (N - 1) * spacing) / 2;

            float[] T0 = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                -center_offset, -center_offset, -center_offset, 1
            };

            offset_x -= (center_offset - w2);
            offset_y -= (center_offset - w2);
            offset_z -= (center_offset - w2);


            float[] T1 = new float[16] {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                -offset_x, -offset_y, -offset_z, 1
            };

            my_multiplyMatrices(ref T, T0);
            if (self)
            {
                my_multiplyMatrices(ref T, T1);
            }

            T2[12] = -T[12];
            T2[13] = -T[13];
            T2[14] = -T[14];

            Dictionary<Axis, float[]> hash = new Dictionary<Axis, float[]>(3);

            hash[Axis.X] = new float[] { 0, 0 };
            hash[Axis.Y] = new float[] { -90, 90 };
            hash[Axis.Z] = new float[] { -90, 0 };

            float ra_angle = hash[axis][0];
            float rb_angle = hash[axis][1];

            float[] RA = setRotateMatrix(ra_angle, Axis.Y);
            float[] RB = setRotateMatrix(rb_angle, Axis.Z);

            float[] RR = setRotateMatrix(angle, Axis.X);

            float[] RAi = setRotateMatrix(-ra_angle, Axis.Y);
            float[] RBi = setRotateMatrix(-rb_angle, Axis.Z);

            my_multiplyMatrices(ref T, RA);
            my_multiplyMatrices(ref T, RB);
            my_multiplyMatrices(ref T, RR);
            my_multiplyMatrices(ref T, RBi);
            my_multiplyMatrices(ref T, RAi);
            my_multiplyMatrices(ref T, T2);

            for (int i = 0; i < edges[k].Length; i++)
            {
                my_multiplyMatrices(ref edges[k][i], T);
            }
            my_multiplyMatrices(ref rotate_matrices[k], T);
        }

        void rotate_fullCube(float angle, Axis axis)
        {
            float[] RR = setRotateMatrix(angle, axis);
            float half_side = (w * N + (N - 1) * spacing) / 2;
            T[12] = -half_side;
            T[13] = -half_side;
            T[14] = -half_side;

            T2[12] = half_side;
            T2[13] = half_side;
            T2[14] = half_side;

            my_multiplyMatrices(ref G_R, RR);

            my_multiplyMatrices(ref axis_x, RR);
            my_multiplyMatrices(ref axis_y, RR);
            my_multiplyMatrices(ref axis_z, RR);

            for (int i = 0; i < edges.Length; i++)
            {
                for (int j = 0; j < edges[i].Length; j++)
                {
                    my_multiplyMatrices(ref edges[i][j], T);
                    my_multiplyMatrices(ref edges[i][j], RR);
                    my_multiplyMatrices(ref edges[i][j], T2);
                }
            }
        }

        void reset()
        {

            float[] matrix;
            float[] imatrix;

            for (int i = 0; i < edges.Length; i++)
            {
                //int n = positions[i];
                matrix = rotate_matrices[i];
                imatrix = invert(matrix);

                for (int j = 0; j < edges[i].Length; j++)
                {
                    my_multiplyMatrices(ref edges[i][j], imatrix);
                }
               
            }
            glControl1.Invalidate();
        }

        
        void drawString(string s, Vector3 position, Color color)
        {
            Vector4 pos = new Vector4(position, 1.0f);
            Matrix4 modelview;
            Matrix4 projection;
            Matrix4 modelviewProjection;
            float[] viewport = new float[4];
            float x, y;

            GL.GetFloat(GetPName.ModelviewMatrix, out modelview);
            GL.GetFloat(GetPName.ProjectionMatrix, out projection);
            GL.GetFloat(GetPName.Viewport, viewport);

            Matrix4.Mult(ref projection, ref modelview, out modelviewProjection);
            Vector4.Transform(ref pos, ref modelviewProjection, out pos);

            x = viewport[2] * (pos.X + 1) / 2.0F;
            y = viewport[3] * (pos.Y + 1) / 2.0F;



            printer.Begin();
            // Hint: use SystemFonts.MessageBoxFont to get the native UI font on Windows.
            printer.Print(s, SystemFonts.MessageBoxFont, color, new RectangleF(x, y, 0, 0));
            printer.End();
        }

        void my_normal(ref float[] a)
        {
            float w = a[3];
            for (int i = 0; i < 4; i++)
            {
                a[i] /= w;
            }
        }

        void cube(int n)
        {


            GL.PushMatrix();

            Hashtable hashtable = new Hashtable();
            for (int i = 2; i <= 26; i += 3)
            {
                hashtable[i] = 1;
            }
            if (!hashtable.ContainsKey(n))
            {
                //return;
            }

            int position = positions[n];
            angleXYZ angle = angles[n];

            List<int> data = getOffsets(position);
            bool in_center = true; // если центральный кубик - выходим, не будем его рисовать 
            foreach (int i in data)
            {
                if (i == 0 || i == (N - 1))
                {
                    in_center = false;
                    break;
                }
            }

            if (in_center)
            {
                return;
            }

            float offset = (w * N + (N - 1) * spacing) / 2;//середина куба
            GL.Translate(
                offset,
                offset,
                offset
            );

            /*GL.Rotate(angle.X, axis_x[0], axis_x[1], axis_x[2]);
            GL.Rotate(angle.Y, axis_y[0], axis_y[1], axis_y[2]);
            GL.Rotate(angle.Z, axis_z[0], axis_z[1], axis_z[2]);*/

            GL.Rotate(angle.X, Vector3.UnitX);
            GL.Rotate(angle.Y, Vector3.UnitY);
            GL.Rotate(angle.Z, Vector3.UnitZ);

            GL.Translate(
                -offset,
                -offset,
                -offset
            );

            float[] v_000 = edges[n][0];
            float[] v_00w = edges[n][1];
            float[] v_0w0 = edges[n][2];
            float[] v_0ww = edges[n][3];
            float[] v_w00 = edges[n][4];
            float[] v_w0w = edges[n][5];
            float[] v_ww0 = edges[n][6];
            float[] v_www = edges[n][7];

            int rem;
            int part = Math.DivRem(n, N * N, out rem);

            #region Cube
            if (n < N * N)
            {
                GL.Color3(Color.Yellow);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_000);
                GL.Vertex3(v_00w);
                GL.Vertex3(v_w0w);
                GL.Vertex3(v_w00);
                GL.End();
            }
			

            if (n >= (N * N * N - N * N))
            {
				GL.Color3(Color.GhostWhite);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_0w0);
                GL.Vertex3(v_0ww);
                GL.Vertex3(v_www);
                GL.Vertex3(v_ww0);
                GL.End();
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, texture_id);
                GL.Enable(EnableCap.Texture2D);
                GL.Color4(Color.White);
                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0, 0);
                GL.Vertex3(v_0w0);
                GL.TexCoord2(0, 1);
                GL.Vertex3(v_0ww);
                GL.TexCoord2(1, 1);
                GL.Vertex3(v_www);
                GL.TexCoord2(1, 0);
                GL.Vertex3(v_ww0);
                GL.End();
            }



            if (n >= (part + 1) * (N * N) - N && n < (part + 1) * (N * N))
            {
				GL.Color3(Color.MediumBlue);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_00w);
                GL.Vertex3(v_w0w);
                GL.Vertex3(v_www);
                GL.Vertex3(v_0ww);
                GL.End();
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, texture_id);
                GL.Enable(EnableCap.Texture2D);
                GL.Color4(Color.White);
                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(0, 1);
                GL.Vertex3(v_00w);
                GL.TexCoord2(1, 1);
                GL.Vertex3(v_w0w);
                GL.TexCoord2(1, 0);
                GL.Vertex3(v_www);
                GL.TexCoord2(0, 0);
                GL.Vertex3(v_0ww);
                //GL.Disable(EnableCap.Texture2D);
                GL.End();
            }

            if (n % N == 0)
            {
				GL.Color3(Color.IndianRed);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_000);
                GL.Vertex3(v_00w);
                GL.Vertex3(v_0ww);
                GL.Vertex3(v_0w0);
                GL.End();
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, texture_id);
                GL.Enable(EnableCap.Texture2D);
                GL.Color4(Color.White);
                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(0, 1);
                GL.Vertex3(v_000);
                GL.TexCoord2(1, 1);
                GL.Vertex3(v_00w);
                GL.TexCoord2(1, 0);
                GL.Vertex3(v_0ww);
                GL.TexCoord2(0, 0);
                GL.Vertex3(v_0w0);
                //GL.Disable(EnableCap.Texture2D);
                GL.End();
            }


            if (n >= N * N * part && n < (N * N * part + N))
            {
                GL.Color3(Color.DarkGreen);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_000);
                GL.Vertex3(v_w00);
                GL.Vertex3(v_ww0);
                GL.Vertex3(v_0w0);
                GL.End();
            }
            else
            {

                GL.Color4(Color.White);
                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(1, 1);
                GL.Vertex3(v_000);
                GL.TexCoord2(0, 1);
                GL.Vertex3(v_w00);
                GL.TexCoord2(0, 0);
                GL.Vertex3(v_ww0);
                GL.TexCoord2(1, 0);
                GL.Vertex3(v_0w0);
                //GL.Disable(EnableCap.Texture2D);
                GL.End();
            }

            if ((n + 1) % N == 0)
            {
				GL.Color3(Color.DarkOrange);
                GL.Begin(BeginMode.Polygon);
                GL.Vertex3(v_w00);
                GL.Vertex3(v_w0w);
                GL.Vertex3(v_www);
                GL.Vertex3(v_ww0);
                GL.End();
            }
            else
            {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, texture_id);
                GL.Color3(Color.White);
                GL.Begin(BeginMode.Quads);


                GL.TexCoord2(1, 1);
                GL.Vertex3(v_w00);
                GL.TexCoord2(0, 1);
                GL.Vertex3(v_w0w);
                GL.TexCoord2(0, 0);
                GL.Vertex3(v_www);
                GL.TexCoord2(1, 0);
                GL.Vertex3(v_ww0);
                //GL.Disable(EnableCap.Texture2D);
                GL.End();
            }
			//////!!!!!!!!!!!!!
            GL.Color3(0, 0, 0);
			GL.LineWidth(4);
            GL.Begin(BeginMode.LineLoop);
            GL.Vertex3(v_000);
            GL.Vertex3(v_w00);
            GL.Vertex3(v_w0w);
            GL.Vertex3(v_00w);           
            GL.End();

            GL.Begin(BeginMode.LineLoop);
            GL.Vertex3(v_0w0);
            GL.Vertex3(v_ww0);
            GL.Vertex3(v_www);
            GL.Vertex3(v_0ww);
            GL.End();

            GL.Begin(BeginMode.Lines);
            GL.Vertex3(v_000);
            GL.Vertex3(v_0w0);
            GL.End();

            GL.Begin(BeginMode.Lines);
            GL.Vertex3(v_w00);
            GL.Vertex3(v_ww0);
            GL.End();

            GL.Begin(BeginMode.Lines);
            GL.Vertex3(v_w0w);
            GL.Vertex3(v_www);
            GL.End();

            GL.Begin(BeginMode.Lines);
            GL.Vertex3(v_00w);
            GL.Vertex3(v_0ww);
            GL.End();
			////////////!!!!!!!!!!!
            #endregion

            GL.PopMatrix();
        }

        List<int> getOffsets(int n)
        {
            List<int> response = new List<int>();
            while (n > 0)
            {
                int i = n % N;
                response.Add(i);
                n -= i;
                n /= N;
            }

            if (response.Count() < 3)
            {
                ulong pad_n = 3 - (ulong)response.Count();
                for (ulong i = 0; i < pad_n; i++)
                {
                    response.Add(0);
                }
            }


            return response;
        }

        Vector3 createVector(Vector3 a, Vector3 b)
        {
            Vector3 c = new Vector3();
            c.X = a.X - b.X;
            c.Y = a.Y - b.Y;
            c.Z = a.Z - b.Z;
            return c;
        }

        Vector3 vectorProduct(Vector3 a, Vector3 b)
        {
            Vector3 VP = new Vector3();
            VP.X = a.Y * b.Z - b.Y * a.Z;
            VP.Y = a.Z * b.X - b.Z * a.X;
            VP.Z = a.X * b.Y - b.Y * a.X;
            return VP;
        }

        float dotProduct(Vector3 a, Vector3 b)
        {
            float SP;
            SP = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            return SP;
        }

        Vector3 planeIntersectLine(Vector3 a, Vector3 b, Vector3 c, Vector3 x, Vector3 y, ref bool NotIntersectPlaneLine)
        {
            Vector3 N, V, W, Point;
            Point = new Vector3();

            float e, d;
            N = new Vector3(0, 0, 1);

            V = createVector(x, a);
            //debug(V, "v");

            d = dotProduct(N, V);
            //debug("d = " + d.ToString());
            W = createVector(x, y);
            //debug(W, "W");
            e = dotProduct(N, W);
            //debug("e = " + e.ToString());

            if (e != 0)
            {
                Point.X = x.X + W.X * d / e;
                Point.Y = x.Y + W.Y * d / e;
                Point.Z = x.Z + W.Z * d / e;
                NotIntersectPlaneLine = false;
            }

            return Point;
        }

        Vector3 planeIntersectLine(Vector3 x, Vector3 y)
        {
            Vector3 Point = new Vector3();
            Vector3 n = new Vector3(0, 0, 1);
            float c = dotProduct(n, y);
            float d = dotProduct(n, x) - this.w * 3 - this.spacing * 2;

            if (c == 0)
            {
                return Point;
            }

            if (d == 0)
            {
                Point = x;
                return Point;
            }

            float k = -d / c;

            Point = x + k * y;

            return Point;
        }

        double line_length(Vector3 a, Vector3 b)
        {
            return Math.Sqrt(Math.Pow((b.X - a.X), 2) + Math.Pow((b.Y - a.Y), 2) + Math.Pow((b.Z - a.Z), 2));
        }

        private void rotatePartFast(EasingTimer.rotateAxisType axis, int rotate_n, int dir)
        {
            int delta;
            int i, j, k, l, m, n;
            int[] new_pos = new int[N * N];
            Dictionary<int, int> pos_to_cube_number = new Dictionary<int, int>(N * N * N);

            for (i = 0; i < N * N * N; i++)
            {
                pos_to_cube_number[positions[i]] = i;
            }

            if (axis == EasingTimer.rotateAxisType.Y)
            {

                i = rotate_n * N * N;

                float angle = -90 * dir;
                for (j = 1; j <= N * N; j++)
                {
                    k = pos_to_cube_number[i];//k - номер кубика                        
                    angles[k].Y = 0;
                    //rotate_cube(k, angle, Axis.Y);
                    rotate_cube(k, angle, Axis.Y, false);
                    i++;
                }

                for (k = 0; k < N; k++)
                {
                    l = N * (N - 1) + k + rotate_n * N * N;//6, 3, 0, 7, 4, 1, 8, 5, 2
                    for (j = 0; j < N; j++)
                    {
                        new_pos[k * N + j] = l;
                        l -= N;
                    }
                }

                if (dir == -1)
                {
                    Array.Reverse(new_pos);
                }

                for (j = 0; j < N * N; j++)
                {
                    i = pos_to_cube_number[j + rotate_n * N * N];//номер кубика на позициях 0, 1, 2, 3, 4, 5, 6, 7, 8
                    l = new_pos[j];
                    positions[i] = l;
                }
            }
            else if (axis == EasingTimer.rotateAxisType.X)
            {
                i = rotate_n;
                delta = N;
                for (j = 1; j <= N * N; j++)
                {
                    //debug("i = " + i.ToString() + ", positions[" + i.ToString() + "] = " + positions[i].ToString() + ", pos_to_cube_number[" + i.ToString() + "] = " + pos_to_cube_number[i].ToString());
                    i += delta;
                }

                i = rotate_n;
                float angle = -90 * dir;
                for (j = 1; j <= N * N; j++)
                {
                    k = pos_to_cube_number[i];//k - номер кубика
                    angles[k].X = 0;
                    //rotate_cube(i, angle, Axis.X);
                    rotate_cube(k, angle, Axis.X, false);
                    i += N;
                }


                for (k = 0; k < N; k++)
                {
                    l = N * N * (N - 1) + k * N + rotate_n;//18, 9, 0, 21, 12, 3, 24, 15, 6
                    for (j = 0; j < N; j++)
                    {
                        new_pos[k * N + j] = l;
                        l -= N * N;
                    }
                }

                if (dir == -1)
                {
                    Array.Reverse(new_pos);
                }

                i = rotate_n;//0, 3, 6, 9, 12, 15, 18, 21, 24,
                for (k = 0; k < N * N; k++)
                {
                    m = pos_to_cube_number[i];//номер кубика на позициях 0, 3, 6, 9, 12, 15, 18, 21, 24,
                    l = new_pos[k];
                    positions[m] = l;//у этого кубика новая позиция                            
                    i += N;
                }
            }
            else if (axis == EasingTimer.rotateAxisType.Z)
            {
                i = rotate_n * N;
                float angle = -90 * dir;
                for (j = 0; j < N; j++)
                {
                    l = i;//0, 1, 2, 9, 10, 11, 18, 19, 20
                    for (k = 0; k < N; k++)
                    {
                        m = pos_to_cube_number[l];//m - номер кубика
                        angles[m].Z = 0;
                        //rotate_cube(l, angle, Axis.Z);
                        rotate_cube(m, angle, Axis.Z, false);
                        l++;
                    }
                    i += N * N;
                }

                for (j = 0; j < N; j++)
                {
                    l = N - j - 1 + rotate_n * N;//2, 11, 20, 1, 10, 19, 0, 9, 18
                    for (k = 0; k < N; k++)
                    {
                        new_pos[j * N + k] = l;
                        l += N * N;
                    }
                }

                if (dir == -1)
                {
                    Array.Reverse(new_pos);
                }

                i = rotate_n * N;
                for (j = 0; j < N; j++)
                {
                    l = i;//0, 1, 2, 9, 10, 11, 18, 19, 20
                    for (k = 0; k < N; k++)
                    {
                        m = pos_to_cube_number[l];//m - номер кубика на позициях 0, 1, 2, 9, 10, 11, 18, 19, 20
                        n = new_pos[j * N + k];//у кубика новая позиция
                        positions[m] = n;
                        l++;
                    }
                    i += N * N;
                }
            }
			int eirwir = 0;
            glControl1.Invalidate();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n2 = 3 * N * 2;
            int part = N * 2;
            Random rand = new Random();

            EasingTimer.rotateAxisType[] axes = new EasingTimer.rotateAxisType[3] {
                EasingTimer.rotateAxisType.X,
                EasingTimer.rotateAxisType.Y,
                EasingTimer.rotateAxisType.Z
            };

            prev_two_div = -1;
            prev_two_mod = -1;
            for (int p = 0; p < 20; p++)
            {
                int r = rand.Next(n2);//n2
                int two_mod;
                int two_div = Math.DivRem(r, 2, out two_mod);
                if (prev_two_div == two_div && prev_two_mod != two_mod)
                {
                    //чтобы не было такого, что одна часть вращается в одну сторону, а потом тут же в другую
                    r = (r + 2) % n2;
                }
                prev_two_div = two_div;
                prev_two_mod = two_mod;


                int axis_n = (int)Math.Floor((double)(r / part));
                EasingTimer.rotateAxisType axis = axes[axis_n];
                r -= axis_n * part;
                int dir;
                int rotate_n = Math.DivRem(r, 2, out dir);
                dir = dir == 0 ? 1 : -1;
                rotatePartFast(axis, rotate_n, dir);
            }
            glControl1.Invalidate();
        }

        private void newToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (G_init_steps > 0)
            {
                return;
            }
            prev_two_div = -1;
            prev_two_mod = -1;
            G_init_steps = 20;
            nextRotate();
        }

        private void nextRotate()
        {
            if (G_init_steps == 0)
            {
                return;
            }
            G_init_steps--;

            int n2 = 3 * N * 2;
            int part = N * 2;
            Random rand = new Random();

            EasingTimer.rotateAxisType[] axes = new EasingTimer.rotateAxisType[3] {
                EasingTimer.rotateAxisType.X,
                EasingTimer.rotateAxisType.Y,
                EasingTimer.rotateAxisType.Z
            };

            int r = rand.Next(n2);
            int two_mod;
            int two_div = Math.DivRem(r, 2, out two_mod);
            if (prev_two_div == two_div && prev_two_mod != two_mod)
            {
                //чтобы не было такого, что одна часть вращается в одну сторону, а потом тут же в другую
                r = (r + 2) % n2;
            }
            prev_two_div = two_div;
            prev_two_mod = two_mod;


            int axis_n = (int)Math.Floor((double)(r / part));
            EasingTimer.rotateAxisType axis = axes[axis_n];
            r -= axis_n * part;
            int dir;
            int rotate_n = Math.DivRem(r, 2, out dir);
            dir = dir == 0 ? 1 : -1;

            EasingTimer.rotate(rotate_n, rotatePart, angles, dir, axis, nextRotate);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
		}

		#endregion


		

		void U() // В
		{
			EasingTimer.rotate(2, rotatePart, angles, -1, EasingTimer.rotateAxisType.Y);
			glControl1.Invalidate();
			Gran temp1 = U_white[0];
			Gran temp2 = U_white[1];
			Gran temp3 = U_white[2];
			Gran temp4 = U_white[5];
			U_white[0] = U_white[6];
			U_white[1] = U_white[3];
			U_white[2] = temp1;
			U_white[5] = temp2;
			U_white[3] = U_white[7];
			U_white[6] = U_white[8];
			U_white[7] = temp4;
			U_white[8] = temp3;
			// поменяли вернюю, меняем дальше
			temp1 = L_pink[0];
			temp2 = L_pink[1];
			temp3 = L_pink[2];
			L_pink[0] = F_blue[0];
			L_pink[1] = F_blue[1];
			L_pink[2] = F_blue[2];
			F_blue[0] = R_orange[0];
			F_blue[1] = R_orange[1];
			F_blue[2] = R_orange[2];
			R_orange[0] = B_green[0];
			R_orange[1] = B_green[1];
			R_orange[2] = B_green[2];
			B_green[0] = temp1;
			B_green[1] = temp2;
			B_green[2] = temp3;
		}
		void U_()
		{
			EasingTimer.rotate(2, rotatePart, angles, 1, EasingTimer.rotateAxisType.Y);
			glControl1.Invalidate();
			Gran temp1 = U_white[0];
			Gran temp2 = U_white[1];
			Gran temp3;
			U_white[0] = U_white[2];
			U_white[1] = U_white[5];
			U_white[2] = U_white[8];
			U_white[5] = U_white[7];
			U_white[8] = U_white[6];
			U_white[6] = temp1;
			U_white[7] = U_white[3];
			U_white[3] = temp2;
			// меням дальше (боковушки)
			temp1 = L_pink[0];
			temp2 = L_pink[1];
			temp3 = L_pink[2];
			L_pink[0] = B_green[0];
			L_pink[1] = B_green[1];
			L_pink[2] = B_green[2];
			B_green[0] = R_orange[0];
			B_green[1] = R_orange[1];
			B_green[2] = R_orange[2];
			R_orange[0] = F_blue[0];
			R_orange[1] = F_blue[1];
			R_orange[2] = F_blue[2];
			F_blue[0] = temp1;
			F_blue[1] = temp2;
			F_blue[2] = temp3;
		}

		void D()
		{
			EasingTimer.rotate(0, rotatePart, angles, 1, EasingTimer.rotateAxisType.Y);
			glControl1.Invalidate();
			Gran temp1 = D_yellow[0];
			Gran temp2 = D_yellow[1];
			Gran temp3 = D_yellow[2];
			Gran temp4 = D_yellow[5];
			D_yellow[0] = D_yellow[6];
			D_yellow[1] = D_yellow[3];
			D_yellow[2] = temp1;
			D_yellow[5] = temp2;
			D_yellow[3] = D_yellow[7];
			D_yellow[6] = D_yellow[8];
			D_yellow[7] = temp4;
			D_yellow[8] = temp3;

			temp1 = L_pink[8];
			temp2 = L_pink[7];
			temp3 = L_pink[6];
			L_pink[8] = B_green[0];
			L_pink[7] = B_green[1];
			L_pink[6] = B_green[2];
			B_green[0] = R_orange[8];
			B_green[1] = R_orange[7];
			B_green[2] = R_orange[6];
			R_orange[8] = F_blue[8];
			R_orange[7] = F_blue[7];
			R_orange[6] = F_blue[6];
			F_blue[8] = temp1;
			F_blue[7] = temp2;
			F_blue[6] = temp3;
			
		}

		void D_()
		{
			EasingTimer.rotate(0, rotatePart, angles, -1, EasingTimer.rotateAxisType.Y);
			glControl1.Invalidate();
			Gran temp1 = D_yellow[0];
			Gran temp2 = D_yellow[1];
			Gran temp3;
			D_yellow[0] = D_yellow[2];
			D_yellow[1] = D_yellow[5];
			D_yellow[2] = D_yellow[8];
			D_yellow[5] = D_yellow[7];
			D_yellow[8] = D_yellow[6];
			D_yellow[6] = temp1;
			D_yellow[7] = D_yellow[3];
			D_yellow[3] = temp2;

			temp1 = L_pink[8];
			temp2 = L_pink[7];
			temp3 = L_pink[6];
			L_pink[8] = F_blue[8];
			L_pink[7] = F_blue[7];
			L_pink[6] = F_blue[6];
			F_blue[8] = R_orange[8];
			F_blue[7] = R_orange[7];
			F_blue[6] = R_orange[6];
			R_orange[8] = B_green[0];
			R_orange[7] = B_green[1];
			R_orange[6] = B_green[2];
			B_green[0] = temp1;
			B_green[1] = temp2;
			B_green[2] = temp3;
		}

		void F()
		{
			EasingTimer.rotate(0, rotatePart, angles, 1, EasingTimer.rotateAxisType.Z);
			glControl1.Invalidate();
			Gran temp1 = F_blue[0];
			Gran temp2 = F_blue[1];
			Gran temp3 = F_blue[2];
			Gran temp4 = F_blue[5];
			F_blue[0] = F_blue[6];
			F_blue[1] = F_blue[3];
			F_blue[2] = temp1;
			F_blue[5] = temp2;
			F_blue[3] = F_blue[7];
			F_blue[6] = F_blue[8];
			F_blue[7] = temp4;
			F_blue[8] = temp3;

			temp1 = L_pink[8];
			temp2 = L_pink[5];
			temp3 = L_pink[2];/// 8 5 2  pink;
/// white 6 7 8; orange 0 3 6; yellow 2 1 0;
			L_pink[8] = D_yellow[2];
			L_pink[5] = D_yellow[1];
			L_pink[2] = D_yellow[0];
			D_yellow[2] = R_orange[0];
			D_yellow[1] = R_orange[3];
			D_yellow[0] = R_orange[6];
			R_orange[0] = U_white[6];
			R_orange[3] = U_white[7];
			R_orange[6] = U_white[8];
			U_white[6] = temp1;
			U_white[7] = temp2;
			U_white[8] = temp3;
		}

		void F_()
		{
			EasingTimer.rotate(0, rotatePart, angles, -1, EasingTimer.rotateAxisType.Z);
			glControl1.Invalidate();
			Gran temp1 = F_blue[0];
			Gran temp2 = F_blue[1];
			Gran temp3;
			F_blue[0] = F_blue[2];
			F_blue[1] = F_blue[5];
			F_blue[2] = F_blue[8];
			F_blue[5] = F_blue[7];
			F_blue[8] = F_blue[6];
			F_blue[6] = temp1;
			F_blue[7] = F_blue[3];
			F_blue[3] = temp2;

			temp1 = L_pink[8];
			temp2 = L_pink[5];
			temp3 = L_pink[2];
			/// 8 5 2  pink;
			/// white 6 7 8; orange 0 3 6; yellow 2 1 0;
			L_pink[8] = U_white[6];
			L_pink[5] = U_white[7];
			L_pink[2] = U_white[8];
			U_white[6] = R_orange[0];
			U_white[7] = R_orange[3];
			U_white[8] = R_orange[6];
			R_orange[0] = D_yellow[2];
			R_orange[3] = D_yellow[1];
			R_orange[6] = D_yellow[0];
			D_yellow[2] = temp1;
			D_yellow[1] = temp2;
			D_yellow[0] = temp3;
		}

		void B()
		{
			EasingTimer.rotate(2, rotatePart, angles, -1, EasingTimer.rotateAxisType.Z);
			glControl1.Invalidate();
			Gran temp1 = B_green[0];
			Gran temp2 = B_green[1];
			Gran temp3 = B_green[2];
			Gran temp4 = B_green[5];
			B_green[0] = B_green[6];
			B_green[1] = B_green[3];
			B_green[2] = temp1;
			B_green[5] = temp2;
			B_green[3] = B_green[7];
			B_green[6] = B_green[8];
			B_green[7] = temp4;
			B_green[8] = temp3;
		//white 2 1 0; pink 0 3 6; yellow 6 7 8; orange 8 5 2
			temp1 = L_pink[0];
			temp2 = L_pink[3];
			temp3 = L_pink[6];
			L_pink[0] = U_white[2];
			L_pink[3] = U_white[1];
			L_pink[6] = U_white[0];
			U_white[2] = R_orange[8];
			U_white[1] = R_orange[5];
			U_white[0] = R_orange[2];
			R_orange[8] = D_yellow[6];
			R_orange[5] = D_yellow[7];
			R_orange[2] = D_yellow[8];
			D_yellow[6] = temp1;
			D_yellow[7] = temp2;
			D_yellow[8] = temp3;
		}

		void B_()
		{
			EasingTimer.rotate(2, rotatePart, angles, 1, EasingTimer.rotateAxisType.Z);
			glControl1.Invalidate();
			Gran temp1 = B_green[0];
			Gran temp2 = B_green[1];
			Gran temp3;
			B_green[0] = B_green[2];
			B_green[1] = B_green[5];
			B_green[2] = B_green[8];
			B_green[5] = B_green[7];
			B_green[8] = B_green[6];
			B_green[6] = temp1;
			B_green[7] = B_green[3];
			B_green[3] = temp2;

			//white 2 1 0; pink 0 3 6; yellow 6 7 8; orange 8 5 2
			temp1 = L_pink[0];
			temp2 = L_pink[3];
			temp3 = L_pink[6];
			L_pink[0] = D_yellow[6];
			L_pink[3] = D_yellow[7];
			L_pink[6] = D_yellow[8];
			D_yellow[6] = R_orange[8];
			D_yellow[7] = R_orange[5];
			D_yellow[8] = R_orange[2];
			R_orange[8] = U_white[2];
			R_orange[5] = U_white[1];
			R_orange[2] = U_white[0];
			U_white[2] = temp1;
			U_white[1] = temp2;
			U_white[0] = temp3;

		}

		void R()
		{
			EasingTimer.rotate(2, rotatePart, angles, -1, EasingTimer.rotateAxisType.X);
			glControl1.Invalidate();
			Gran temp1 = R_orange[0];
			Gran temp2 = R_orange[1];
			Gran temp3 = R_orange[2];
			Gran temp4 = R_orange[5];
			R_orange[0] = R_orange[6];
			R_orange[1] = R_orange[3];
			R_orange[2] = temp1;
			R_orange[5] = temp2;
			R_orange[3] = R_orange[7];
			R_orange[6] = R_orange[8];
			R_orange[7] = temp4;
			R_orange[8] = temp3;

		//// white 8 5 2; blue 8 5 2; yellow 8 5 2; green 8 5 2;
			temp1 = U_white[8];
			temp2 = U_white[5];
			temp3 = U_white[2];
			U_white[8] = F_blue[8];
			U_white[5] = F_blue[5];
			U_white[2] = F_blue[2];
			F_blue[8] = D_yellow[8];
			F_blue[5] = D_yellow[5];
			F_blue[2] = D_yellow[2];
			D_yellow[8] = B_green[8];
			D_yellow[5] = B_green[5];
			D_yellow[2] = B_green[2];
			B_green[8] = temp1;
			B_green[5] = temp2;
			B_green[2] = temp3;
		}

		void R_()
		{
			EasingTimer.rotate(2, rotatePart, angles, 1, EasingTimer.rotateAxisType.X);
			glControl1.Invalidate();
			Gran temp1 = R_orange[0];
			Gran temp2 = R_orange[1];
			Gran temp3;
			R_orange[0] = R_orange[2];
			R_orange[1] = R_orange[5];
			R_orange[2] = R_orange[8];
			R_orange[5] = R_orange[7];
			R_orange[8] = R_orange[6];
			R_orange[6] = temp1;
			R_orange[7] = R_orange[3];
			R_orange[3] = temp2;

			//// white 8 5 2; blue 8 5 2; yellow 8 5 2; green 8 5 2;
			temp1 = U_white[8];
			temp2 = U_white[5];
			temp3 = U_white[2];
			U_white[8] = B_green[8];
			U_white[5] = B_green[5];
			U_white[2] = B_green[2];
			B_green[8] = D_yellow[8];
			B_green[5] = D_yellow[5];
			B_green[2] = D_yellow[2];
			D_yellow[8] = F_blue[8];
			D_yellow[5] = F_blue[5];
			D_yellow[2] = F_blue[2];
			F_blue[8] = temp1;
			F_blue[5] = temp2;
			F_blue[2] = temp3;

		}

		void L()
		{
			EasingTimer.rotate(0, rotatePart, angles, 1, EasingTimer.rotateAxisType.X);
			glControl1.Invalidate();
			Gran temp1 = L_pink[0];
			Gran temp2 = L_pink[1];
			Gran temp3 = L_pink[2];
			Gran temp4 = L_pink[5];
			L_pink[0] = L_pink[6];
			L_pink[1] = L_pink[3];
			L_pink[2] = temp1;
			L_pink[5] = temp2;
			L_pink[3] = L_pink[7];
			L_pink[6] = L_pink[8];
			L_pink[7] = temp4;
			L_pink[8] = temp3;

			//// white 0 3 6; blue 0 3 6; yellow 0 3 6; green 0 3 6;
			temp1 = U_white[0];
			temp2 = U_white[3];
			temp3 = U_white[6];
			U_white[0] = B_green[0];
			U_white[3] = B_green[3];
			U_white[6] = B_green[6];
			B_green[0] = D_yellow[0];
			B_green[3] = D_yellow[3];
			B_green[6] = D_yellow[6];
			D_yellow[0] = F_blue[0];
			D_yellow[3] = F_blue[3];
			D_yellow[6] = F_blue[6];
			F_blue[0] = temp1;
			F_blue[3] = temp2;
			F_blue[6] = temp3;

		}

		void L_()
		{

			EasingTimer.rotate(0, rotatePart, angles, -1, EasingTimer.rotateAxisType.X);
			glControl1.Invalidate();
			Gran temp1 = L_pink[0];
			Gran temp2 = L_pink[1];
			Gran temp3;
			L_pink[0] = L_pink[2];
			L_pink[1] = L_pink[5];
			L_pink[2] = L_pink[8];
			L_pink[5] = L_pink[7];
			L_pink[8] = L_pink[6];
			L_pink[6] = temp1;
			L_pink[7] = L_pink[3];
			L_pink[3] = temp2;

			//// white 0 3 6; blue 0 3 6; yellow 0 3 6; green 0 3 6;
			temp1 = U_white[0];
			temp2 = U_white[3];
			temp3 = U_white[6];
			U_white[0] = F_blue[0];
			U_white[3] = F_blue[3];
			U_white[6] = F_blue[6];
			F_blue[0] = D_yellow[0];
			F_blue[3] = D_yellow[3];
			F_blue[6] = D_yellow[6];
			D_yellow[0] = B_green[0];
			D_yellow[3] = B_green[3];
			D_yellow[6] = B_green[6];
			B_green[0] = temp1;
			B_green[3] = temp2;
			B_green[6] = temp3;
		}

		private void пошароваToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// U U_ D D_ F F_ B B_ R R_ L L_

		}

		
	}


    public class angleXYZ
    {
        public angleXYZ()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class Plane
    {
        private Vector3[] _points;
        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            if (a == b || a == c || b == c)
            {
                throw new System.Exception("a == b || a == c || b == c");
            }
           
            _points = new Vector3[3] { a, b, c};
        }

        public Vector3[] points
        {
            get { return this._points; }
        }
		public static bool Equal(Plane p1, Plane p2)
		{ 
			return (p1._points[0] == p2._points[0] && p1._points[1] == p2._points[1] && p1._points[2] == p2._points[2]); 
		}
    }

    public class EasingTimer : System.Timers.Timer
    {
        private static EasingTimer t;

        public bool _run = false;
        public DateTime begin_time;
        public int duration = 500;
        public int firstAngle = 0;
        public int rotateN = 0;
        public ElapsedEventHandler func;
        public enum rotateAxisType { X, Y, Z };
        public rotateAxisType rotateAxis;
        public int dir;

        public delegate void TimerFunction(object sender, ElapsedEventArgs e);
        public delegate void timerUPFunction();
        public timerUPFunction funcUp;

        private EasingTimer(int n, TimerFunction func, List<angleXYZ> angles, int dir, rotateAxisType rotateAxis, timerUPFunction funcUp = null)
            : base(100)
        {
            this.firstAngle = 0;
            this.rotateN = n;
            this.begin_time = DateTime.Now;
            this.func = new ElapsedEventHandler(func);
            this.Elapsed += this.func;
            this._run = false;
            this.dir = dir;
            this.rotateAxis = rotateAxis;
            this.funcUp = funcUp;
        }

        public static void rotate(int n, TimerFunction func, List<angleXYZ> angles, int dir, rotateAxisType rotateAxis, timerUPFunction funcUp = null)
        {
            if (t == null)
            {
                //MessageBox.Show("firstAngle = " + firstAngle.ToString());
                t = new EasingTimer(n, func, angles, dir, rotateAxis, funcUp);
                t.rotateAxis = rotateAxis;
            }
            else
            {
                //если прокручивание работает, то просто возвращаем таймер
                //если прокручинваие уже не работает, то заново инициализируем переменные, как бы вызываем конструктор
                if (!t._run)
                {
                    //MessageBox.Show("2: firstAngle = " + firstAngle.ToString());
                    t.firstAngle = 0;
                    t.rotateN = n;
                    t.begin_time = DateTime.Now;
                    t.Elapsed -= t.func;
                    t.func = new ElapsedEventHandler(func);
                    t.Elapsed += t.func;
                    t.dir = dir;
                    t.rotateAxis = rotateAxis;
                    t.funcUp = funcUp;
                }
            }

            //Если уже пошёл процесс кручения, то выходим
            if (t._run)
            {
                return;
            }

            t._run = true;
            t.Start();
        }

        public bool run
        {
            get
            {
                return _run;
            }

            set
            {
                bool prev_run = this._run;
                this._run = value;
                if (prev_run && !value)
                {
                    if (this.funcUp != null)
                    {
                        this.funcUp();
                    }
                }
            }
        }
    }

    public class ViewPoint
    {
        private float _viewX;
        private float _viewY;
        private float _viewZ;
        private float _l = 100;
        private float _angle_view_beta = 25;
        private float _angle_view_alfa = -10;
        private float _orintation_y = 1;
        private float _orintation_hor = 1;
        private Form1.Axis nearestAxis;

        public ViewPoint()
        {
            _angle_view_beta = 25;
            _angle_view_alfa = -10;
            _orintation_y = 1;

            setView();
        }

        public float viewX
        {
            get { return this._viewX; }
        }

        public float viewY
        {
            get { return this._viewY; }
        }

        public float viewZ
        {
            get { return this._viewZ; }
        }

        public float orintation_y
        {
            get { return this._orintation_y; }
        }

        public float orintation_hor
        {
            get { return this._orintation_hor; }
        }

        public float angle_view_beta
        {
            set
            {
                //debug("angle_view_beta = " + angle_view_beta);
                _angle_view_beta = value;
                if (_angle_view_beta > 360)
                {
                    _angle_view_beta %= 360;
                }
                else if (_angle_view_beta < 0)
                {
                    double n = Math.Floor(Math.Abs(_angle_view_beta) / 360);
                    _angle_view_beta += (float)n * 360;
                    _angle_view_beta = 360 + _angle_view_beta;
                }

                setView();
                if (_angle_view_beta < 90 || _angle_view_beta > 270)
                {
                    _orintation_y = 1;
                }
                else
                {
                    _orintation_y = -1;
                }
            }

            get
            {
                return _angle_view_beta;
            }
        }

        public float angle_view_alfa
        {
            set
            {
                _angle_view_alfa = value;
                if (_angle_view_alfa > 360)
                {
                    _angle_view_alfa %= 360;
                }
                else if (_angle_view_alfa < 0)
                {
                    _angle_view_alfa = (float)-Math.Floor(Math.Abs(_angle_view_alfa) % 360);
                    _angle_view_alfa = 360 + _angle_view_alfa;
                }

                setView();
                if (_angle_view_alfa > 315 || _angle_view_alfa < 135)
                {
                    _orintation_hor = 1;
                }
                else
                {
                    _orintation_hor = -1;
                }

                if (_angle_view_beta > 90 && _angle_view_beta < 270)
                {
                    _orintation_hor *= -1;
                }
            }

            get
            {
                return _angle_view_alfa;
            }
        }

        private void setView()
        {
            float angle_beta_rad = (float)(_angle_view_beta * Math.PI / 180);
            float angle_alfa_rad = (float)(_angle_view_alfa * Math.PI / 180);

            _viewY = (float)(_l * Math.Sin(angle_beta_rad));
            float viewX2plusviewZ2 = (float)(_l * Math.Cos(angle_beta_rad));
            _viewX = (float)(viewX2plusviewZ2 * Math.Sin(angle_alfa_rad));
            _viewZ = (float)(viewX2plusviewZ2 * Math.Cos(angle_alfa_rad));

            double angle_x = Math.Acos(Math.Abs(_viewX) / _l) * 180 / Math.PI;
            double angle_y = Math.Acos(Math.Abs(_viewY) / _l) * 180 / Math.PI;
            double angle_z = Math.Acos(Math.Abs(_viewZ) / _l) * 180 / Math.PI;

            if (angle_x <= angle_y && angle_x <= angle_z)
            {
                nearestAxis = Form1.Axis.X;
            }
            else if (angle_y <= angle_x && angle_y <= angle_z)
            {
                nearestAxis = Form1.Axis.Y;
            }
            else if (angle_z <= angle_x && angle_z <= angle_y)
            {
                nearestAxis = Form1.Axis.Z;
            }
        }

        public Form1.Axis getNearestAxis()
        {
            return nearestAxis;
        }
    }
}


