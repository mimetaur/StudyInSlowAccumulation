
// All code copyright Chris West 2011.
// If you make any improvements please send them back to me at chris@west-racing.com so I can update the package.
using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;

public enum IMGFormat
{
	Tga,
	Jpg,
}

public class MegaGrab : MonoBehaviour
{
	public Camera				SrcCamera			= null;				// camera to use for screenshot
	public KeyCode				GrabKey				= KeyCode.S;		// Key to grab the screenshot
	public int					ResUpscale			= 1;				// How much to increase the screen shot res by
	public float				Blur				= 1.0f;				// Pixel oversampling
	public int					AASamples			= 8;				// Anti aliasing samples
	public AnisotropicFiltering	FilterMode			= AnisotropicFiltering.ForceEnable;	// Filter mode
	public bool					UseJitter			= false;			// use random jitter for AA sampling
	public string				SaveName			= "Grab";			// Base name for grabs
	public string				Format				= "dddd MMM dd yyyy HH_mm_ss";	// format string for date time info
	public string				Enviro				= "";				// Enviro variable to use ie USERPROFILE
	public string				Path				= "";
	public bool					UseDOF				= false;			// Use Dof grab
	public float				focalDistance		= 8.0f;				// DOF focal distance
	public int					totalSegments		= 8;				// How many DOF samples
	public float				sampleRadius		= 1.0f;				// Amount of DOF effect
	public bool					CalcFromSize		= false;			// Let grab calc res from dpi and Width(in inches)
	public int					Dpi					= 300;				// Number of Dots per inch required
	public float				Width				= 24.0f;			// Final physical size of grab using Dpi
	public int					NumberOfGrabs		= 0;				// Read only of how many grabs will happen
	public float				EstimatedTime		= 0.0f;				// Guide to show how long a grab will take in Seconds
	public int					GrabWidthWillBe		= 0;				// Width of final image
	public int					GrabHeightWillBe	= 0;				// Height of final IMage
	public bool					UseCoroutine		= true;				// Use coroutine method, needed for later versions of unity
	public IMGFormat			OutputFormat		= IMGFormat.Jpg;
	public float				Quality				= 75.0f;
	public bool					uploadGrabs			= false;
	public string				m_URL				= "";
	public bool					sequenceGrab		= false;
	public int					framerate			= 30;
	public float				grabTime			= 5.0f;
	public KeyCode				CancelKey			= KeyCode.C;
	public bool					usemask				= true;
	public Color				alphamaskcol;
	public bool					alphagrab			= false;
	public bool					DoGrab				= false;
	public bool					CancelSeqGrab		= false;
	public bool					adddatetime			= true;
	public bool					grabFromStart		= false;

	float						mleft;
	float						mright;
	float						mtop;
	float						mbottom;
	int							sampcount;
	Vector2[]					poisson;
	Texture2D					grabtex;
	Color[,]					accbuf;
	Color[,]					blendbuf;
	byte[]						output1;
	Color[]						outputjpg;
	AnisotropicFiltering		filtering;
	MGBlendTable				blendtable;
	int							DOFSamples;
	Vector3						camfor;
	Vector3						campos;
	Matrix4x4					camtm;
	bool						doingSeqGrab		= false;
	int							framenumber			= 0;
	float						gtime				= 0.0f;

	public void DoScreenGrab()
	{
		DoGrab = true;
	}

	public void CancelGrab()
	{
		CancelSeqGrab = true;
	}

	// Calc the camera offsets and rots in init
	void CalcDOFInfo(Camera camera)
	{
		camtm = camera.transform.localToWorldMatrix;
		campos = camera.transform.position;
		camfor = camera.transform.forward;
	}

	void ChangeDOFPos(int segment)
	{
		float theta = (float)segment / (float)(totalSegments) * Mathf.PI * 2.0f;
		float radius = sampleRadius;

		float uOffset = radius * Mathf.Cos(theta);
		float vOffset = radius * Mathf.Sin(theta);

		Vector3 newCameraLocation = new Vector3(uOffset, vOffset, 0.0f);
		Vector3 initialTargetLocation = camfor * focalDistance;	//new Vector3(0.0f, 0.0f, -focalDistance);

		Vector3 tpos = initialTargetLocation + campos;	//srcCamera.transform.TransformPoint(initialTargetLocation);
		SrcCamera.transform.position = camtm.MultiplyPoint3x4(newCameraLocation);
		SrcCamera.transform.LookAt(tpos);
	}

	static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
	{
		Matrix4x4 m = Matrix4x4.identity;

		m[0, 0] = (2.0f * near) / (right - left);
		m[1, 1] = (2.0f * near) / (top - bottom);
		m[0, 2] = (right + left) / (right - left);
		m[1, 2] = (top + bottom) / (top - bottom);
		m[2, 2] = -(far + near) / (far - near);
		m[2, 3] = -(2.0f * far * near) / (far - near);
		m[3, 2] = -1.0f;
		m[3, 3] = 0.0f;
		return m;
	}

	Matrix4x4 CalcProjectionMatrix(float left, float right, float bottom, float top, float near, float far, float xoff, float yoff)
	{
		float scalex = (right - left) / (float)Screen.width;
		float scaley = (top - bottom) / (float)Screen.height;

		return PerspectiveOffCenter(left - xoff * scalex, right - xoff * scalex, bottom - yoff * scaley, top - yoff * scaley, near, far);
	}

	void Cleanup()
	{
		QualitySettings.anisotropicFiltering = filtering;
		
		output1 = null;
		outputjpg = null;
		blendtable = null;
		accbuf = null;
		blendbuf = null;

		System.GC.Collect();
	}

	bool InitGrab(int width, int height, int aasamples)
	{
		blendtable = new MGBlendTable(32, 32, totalSegments, 0.4f, true);

		if ( ResUpscale < 1 )
			ResUpscale = 1;

		if ( AASamples < 1 )
			AASamples = 1;

		if ( SrcCamera == null )
			SrcCamera = Camera.main;

		if ( SrcCamera == null )
		{
			Debug.Log("No Camera set as source and no main camera found in the scene");
			return false;
		}
		CalcDOFInfo(SrcCamera);

		if ( OutputFormat == IMGFormat.Tga )
		{
			if ( alphagrab )
				output1 = new byte[(width * ResUpscale) * (height * ResUpscale) * 4];
			else
				output1 = new byte[(width * ResUpscale) * (height * ResUpscale) * 3];
		}
		else
			outputjpg = new Color[(width * ResUpscale) * (height * ResUpscale)];

		if ( output1 != null || outputjpg != null )
		{
			filtering = QualitySettings.anisotropicFiltering;
			QualitySettings.anisotropicFiltering = FilterMode;

			if ( alphagrab )
				grabtex = new Texture2D(width, height, TextureFormat.ARGB32, false);
			else
				grabtex = new Texture2D(width, height, TextureFormat.RGB24, false);

			if ( grabtex != null )
			{
				accbuf = new Color[width, height];
				blendbuf = new Color[width, height];

				if ( accbuf != null )
				{
					float l = (1.0f - Blur) * 0.5f;
					float h = 1.0f + ((Blur - 1.0f) * 0.5f);

					if ( UseJitter)
					{
						poisson = new Vector2[aasamples];

						sampcount = aasamples;
						for ( int i = 0; i < aasamples; i++ )
						{
							Vector2 pos = new Vector2();
							pos.x = Mathf.Lerp(l, h, UnityEngine.Random.value);
							pos.y = Mathf.Lerp(l, h, UnityEngine.Random.value);
							poisson[i] = pos;
						}
					}
					else
					{
						int samples = (int)Mathf.Sqrt((float)aasamples);
						if ( samples < 1 )
							samples = 1;

						sampcount = samples * samples;

						poisson = new Vector2[samples * samples];

						int i = 0;

						for ( int ya = 0; ya < samples; ya++ )
						{
							for ( int xa = 0; xa < samples; xa++ )
							{
								float xa1 = ((float)xa / (float)samples);
								float ya1 = ((float)ya / (float)samples);

								Vector2 pos = new Vector2();
								pos.x = Mathf.Lerp(l, h, xa1);
								pos.y = Mathf.Lerp(l, h, ya1);
								poisson[i++] = pos;
							}
						}
					}

					return true;
				}
			}
		}

		Debug.Log("Cant create a large enough texture, Try lower ResUpscale value");
		return false;
	}

	Texture2D GrabImage(int samples, float x, float y)
	{
		float ps = 1.0f / (float)ResUpscale;

		for ( int i = 0; i < sampcount; i++ )
		{
			float xa = poisson[i].x * ps;
			float ya = poisson[i].y * ps;

			// Move view and grab
			float xo = x + xa;
			float yo = y + ya;

			//SrcCamera.projectionMatrix = CalcProjectionMatrix(mleft, mright, mbottom, mtop, SrcCamera.near, SrcCamera.far, xo, yo);
			SrcCamera.projectionMatrix = CalcProjectionMatrix(mleft, mright, mbottom, mtop, SrcCamera.nearClipPlane, SrcCamera.farClipPlane, xo, yo);

			SrcCamera.Render();

			// Read screen contents into the texture
			grabtex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			grabtex.Apply();

			if ( i == 0 )
			{
				for ( int ty = 0; ty < Screen.height; ty++ )
				{
					for ( int tx = 0; tx < Screen.width; tx++ )
						accbuf[tx, ty] = grabtex.GetPixel(tx, ty);
				}
			}
			else
			{
				for ( int ty = 0; ty < Screen.height; ty++ )
				{
					for ( int tx = 0; tx < Screen.width; tx++ )
						accbuf[tx, ty] += grabtex.GetPixel(tx, ty);
				}
			}
		}

		for ( int ty = 0; ty < Screen.height; ty++ )
		{
			for ( int tx = 0; tx < Screen.width; tx++ )
				grabtex.SetPixel(tx, ty, accbuf[tx, ty] / sampcount);
		}

		grabtex.Apply();

		return grabtex;
	}

	Texture2D GrabImageAlpha(int samples, float x, float y)
	{
		float ps = 1.0f / (float)ResUpscale;

		byte ab = (byte)(alphamaskcol.b * 255.0f);
		byte ag = (byte)(alphamaskcol.g * 255.0f);
		byte ar = (byte)(alphamaskcol.r * 255.0f);

		for ( int i = 0; i < sampcount; i++ )
		{
			float xa = poisson[i].x * ps;
			float ya = poisson[i].y * ps;

			// Move view and grab
			float xo = x + xa;
			float yo = y + ya;

			SrcCamera.projectionMatrix = CalcProjectionMatrix(mleft, mright, mbottom, mtop, SrcCamera.nearClipPlane, SrcCamera.farClipPlane, xo, yo);
			SrcCamera.Render();

			// Read screen contents into the texture
			grabtex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			grabtex.Apply();

			if ( i == 0 )
			{
				for ( int ty = 0; ty < Screen.height; ty++ )
				{
					for ( int tx = 0; tx < Screen.width; tx++ )
					{
						Color c = grabtex.GetPixel(tx, ty);

						if ( usemask )
						{
							byte b = (byte)(c.b * 255.0f);
							byte g = (byte)(c.g * 255.0f);
							byte r = (byte)(c.r * 255.0f);

							if ( r == ar && g == ag && b == ab )
							{
								c = Color.black;
								c.a = 0.0f;
							}
							else
								c.a = 1.0f;
						}
						else
						{
							if ( c.a == 0.0f )
								c.r = c.g = c.b = 0.0f;
						}

						accbuf[tx, ty] = c;	//grabtex.GetPixel(tx, ty);
					}
				}
			}
			else
			{
				for ( int ty = 0; ty < Screen.height; ty++ )
				{
					for ( int tx = 0; tx < Screen.width; tx++ )
					{
						Color c = grabtex.GetPixel(tx, ty);

						if ( usemask )
						{
							byte b = (byte)(c.b * 255.0f);
							byte g = (byte)(c.g * 255.0f);
							byte r = (byte)(c.r * 255.0f);

							if ( r == ar && g == ag && b == ab )
							{
								c = Color.black;
								c.a = 0.0f;
							}
							else
								c.a = 1.0f;
						}
						else
						{
							if ( c.a == 0.0f )
								c.r = c.g = c.b = 0.0f;
						}

						accbuf[tx, ty] += c;	//grabtex.GetPixel(tx, ty);
					}
				}
			}
		}

		for ( int ty = 0; ty < Screen.height; ty++ )
		{
			for ( int tx = 0; tx < Screen.width; tx++ )
				grabtex.SetPixel(tx, ty, accbuf[tx, ty] / sampcount);
		}

		grabtex.Apply();

		return grabtex;
	}

	void GrabAA(float x, float y)
	{
		float ps = 1.0f / (float)ResUpscale;

		for ( int ty = 0; ty < Screen.height; ty++ )
		{
			for ( int tx = 0; tx < Screen.width; tx++ )
				accbuf[tx, ty] = Color.black;
		}

		for ( int i = 0; i < sampcount; i++ )
		{
			float xa = poisson[i].x * ps;
			float ya = poisson[i].y * ps;

			// Move view and grab
			float xo = x + xa;
			float yo = y + ya;

			SrcCamera.projectionMatrix = CalcProjectionMatrix(mleft, mright, mbottom, mtop, SrcCamera.nearClipPlane, SrcCamera.farClipPlane, xo, yo);
			SrcCamera.Render();

			// Read screen contents into the texture
			grabtex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			grabtex.Apply();

			for ( int ty = 0; ty < Screen.height; ty++ )
			{
				for ( int tx = 0; tx < Screen.width; tx++ )
					accbuf[tx, ty] += grabtex.GetPixel(tx, ty);
			}
		}

		for ( int ty = 0; ty < Screen.height; ty++ )
		{
			for ( int tx = 0; tx < Screen.width; tx++ )
				accbuf[tx, ty] = accbuf[tx, ty] / sampcount;
		}
	}

	// return accbuf here 
	Texture2D GrabImageDOF(int samples, float x, float y)
	{
		for ( int ty = 0; ty < Screen.height; ty++ )
		{
			for ( int tx = 0; tx < Screen.width; tx++ )
				blendbuf[tx, ty] = Color.black;
		}

		for ( int d = 0; d < totalSegments; d++ )
		{
			ChangeDOFPos(d);
			GrabAA(x, y);

			// Blend image
			blendtable.BlendImages(blendbuf, accbuf, Screen.width, Screen.height, d);
		}

		return grabtex;
	}

	void DoGrabTGA()
	{
		if ( alphagrab )
		{
			DoGrabTGAAlpha();
			return;
		}

		if ( InitGrab(Screen.width, Screen.height, AASamples) )
		{
			mtop		= SrcCamera.nearClipPlane * Mathf.Tan(SrcCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			mbottom	= -mtop;
			mleft		= mbottom * SrcCamera.aspect;
			mright	= mtop * SrcCamera.aspect;

			int width = Screen.width;
			int height = Screen.height;

			if ( AASamples < 1 )
				AASamples = 1;

			int count = 0;

			for ( int y = 0; y < ResUpscale; y++ )
			{
				float yo = (float)y / (float)ResUpscale;

				for ( int x = 0; x < ResUpscale; x++ )
				{
					count++;
					float xo = (float)x / (float)ResUpscale;

					if ( UseDOF )
					{
						GrabImageDOF(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = blendbuf[w, h];

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1)) * 3;
								output1[ix + 0] = (byte)(col.b * 255.0f);
								output1[ix + 1] = (byte)(col.g * 255.0f);
								output1[ix + 2] = (byte)(col.r * 255.0f);
							}
						}
					}
					else
					{
						GrabImage(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = grabtex.GetPixel(w, h);

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1)) * 3;

								output1[ix + 0] = (byte)(col.b * 255.0f);
								output1[ix + 1] = (byte)(col.g * 255.0f);
								output1[ix + 2] = (byte)(col.r * 255.0f);
							}
						}
					}
				}
			}

			Destroy(grabtex);
			grabtex = null;

			string epath = "";
			if ( Enviro != null && Enviro.Length > 0 )
				epath = System.Environment.GetEnvironmentVariable(Enviro);
			else
				epath = Directory.GetCurrentDirectory();

			string fname = "";

			if ( doingSeqGrab )
			{
				if ( adddatetime )
					fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format) + " " + framenumber.ToString("0000");
				else
					fname = SaveName + framenumber.ToString("0000");
			}
			else
				fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format);

			fname += ".tga";

			fname = System.IO.Path.Combine(Path, fname);
			fname = System.IO.Path.Combine(epath, fname);

			if ( fname[0] == '\\' )
				fname = fname.Remove(0, 1);

			SaveTGA(fname, (width * ResUpscale), (height * ResUpscale), output1, alphagrab);

			SrcCamera.ResetProjectionMatrix();
			Cleanup();
		}
	}

	void DoGrabTGAAlpha()
	{
		if ( InitGrab(Screen.width, Screen.height, AASamples) )
		{
			mtop = SrcCamera.nearClipPlane * Mathf.Tan(SrcCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			mbottom = -mtop;
			mleft = mbottom * SrcCamera.aspect;
			mright = mtop * SrcCamera.aspect;

			int width = Screen.width;
			int height = Screen.height;

			if ( AASamples < 1 )
				AASamples = 1;

			int count = 0;

			for ( int y = 0; y < ResUpscale; y++ )
			{
				float yo = (float)y / (float)ResUpscale;

				for ( int x = 0; x < ResUpscale; x++ )
				{
					count++;
					float xo = (float)x / (float)ResUpscale;

					if ( UseDOF )
					{
						GrabImageDOF(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = blendbuf[w, h];	//tex.GetPixel(w, h);

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1)) * 4;
								output1[ix + 0] = (byte)(col.b * 255.0f);
								output1[ix + 1] = (byte)(col.g * 255.0f);
								output1[ix + 2] = (byte)(col.r * 255.0f);
								output1[ix + 3] = (byte)(col.a * 255.0f);
							}
						}
					}
					else
					{
						GrabImageAlpha(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = grabtex.GetPixel(w, h);

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1)) * 4;

								output1[ix + 0] = (byte)(col.b * 255.0f);
								output1[ix + 1] = (byte)(col.g * 255.0f);
								output1[ix + 2] = (byte)(col.r * 255.0f);
								output1[ix + 3] = (byte)(col.a * 255.0f);
							}
						}
					}
				}
			}

			Destroy(grabtex);
			grabtex = null;

			string epath = "";
			if ( Enviro != null && Enviro.Length > 0 )
				epath = System.Environment.GetEnvironmentVariable(Enviro);
			else
				epath = Directory.GetCurrentDirectory();

			string fname = "";

			if ( doingSeqGrab )
			{
				if ( adddatetime )
					fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format) + " " + framenumber.ToString("0000");
				else
					fname = SaveName + framenumber.ToString("0000");
			}
			else
				fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format);

			fname += ".tga";

			fname = System.IO.Path.Combine(Path, fname);
			fname = System.IO.Path.Combine(epath, fname);

			if ( fname[0] == '\\' )
				fname = fname.Remove(0, 1);

			SaveTGA(fname, (width * ResUpscale), (height * ResUpscale), output1, alphagrab);

			SrcCamera.ResetProjectionMatrix();
			Cleanup();
		}
	}

	void DoGrabJPG()
	{
		if ( InitGrab(Screen.width, Screen.height, AASamples) )
		{
			mtop = SrcCamera.nearClipPlane * Mathf.Tan(SrcCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			mbottom = -mtop;
			mleft = mbottom * SrcCamera.aspect;
			mright = mtop * SrcCamera.aspect;

			int width = Screen.width;
			int height = Screen.height;

			if ( AASamples < 1 )
				AASamples = 1;

			int count = 0;

			for ( int y = 0; y < ResUpscale; y++ )
			{
				float yo = (float)y / (float)ResUpscale;

				for ( int x = 0; x < ResUpscale; x++ )
				{
					count++;
					float xo = (float)x / (float)ResUpscale;

					if ( UseDOF )
					{
						GrabImageDOF(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = blendbuf[w, h];

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1));
								outputjpg[ix] = col;
							}
						}
					}
					else
					{
						GrabImage(AASamples, xo, yo);
						for ( int h = 0; h < Screen.height; h++ )
						{
							int index = ((ResUpscale - y) + (h * ResUpscale) - 1) * (width * ResUpscale);

							for ( int w = 0; w < Screen.width; w++ )
							{
								Color col = grabtex.GetPixel(w, h);

								int ix = (index + ((ResUpscale - x) + (w * ResUpscale) - 1));
								outputjpg[ix] = col;
							}
						}
					}
				}
			}

			Destroy(grabtex);
			grabtex = null;
#if false
			string epath = "";
			if ( Enviro != null && Enviro.Length > 0 )
				epath = System.Environment.GetEnvironmentVariable(Enviro);
			else
				epath = Directory.GetCurrentDirectory();

			string fname = "";

			if ( doingSeqGrab )
			{
				if ( adddatetime )
					fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format) + " " + framenumber.ToString("0000");
				else
					fname = SaveName + framenumber.ToString("0000");
			}
			else
				fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format);

			fname += ".jpg";

			fname = System.IO.Path.Combine(Path, fname);
			fname = System.IO.Path.Combine(epath, fname);

			if ( fname[0] == '\\' )
				fname = fname.Remove(0, 1);
#endif
			string epath = "";
			if ( Enviro != null && Enviro.Length > 0 )
				epath = System.Environment.GetEnvironmentVariable(Enviro);
			else
				epath = Directory.GetCurrentDirectory();

			if ( uploadGrabs )
			{
				string fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format);
				UploadJPG(fname + ".jpg", (width * ResUpscale), (height * ResUpscale), outputjpg);
			}
			else
			{
				string fname = "";

				if ( doingSeqGrab )
				{
					if ( adddatetime )
						fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format) + " " + framenumber.ToString("0000");
					else
						fname = SaveName + framenumber.ToString("0000");
				}
				else
					fname = SaveName + " " + (width * ResUpscale) + "x" + (height * ResUpscale) + " " + System.DateTime.Now.ToString(Format);

				fname += ".jpg";

				fname = System.IO.Path.Combine(Path, fname);
				fname = System.IO.Path.Combine(epath, fname);

				if ( fname[0] == '\\' )
					fname = fname.Remove(0, 1);

				//Debug.Log("Saving jpg to \"" + fname + ".jpg\"");
				SaveJPG(fname, (width * ResUpscale), (height * ResUpscale), outputjpg);
			}
			SrcCamera.ResetProjectionMatrix();
			Cleanup();
		}
	}

	void SaveJPGOld(string filename, int width, int height, Color[] pixels)
	{
		FileStream fs = new FileStream(filename, FileMode.Create);
		if ( fs != null )
		{
			BinaryWriter bw = new BinaryWriter(fs);

			if ( bw != null )
			{
				Quality = Mathf.Clamp(Quality, 0.0f, 100.0f);
				JPGEncoder NewEncoder = new JPGEncoder(pixels, width, height, Quality);
				NewEncoder.doEncoding();
				byte[] TexData = NewEncoder.GetBytes();

				bw.Write(TexData);
				bw.Close();
			}
			fs.Close();
		}
		else
		{

		}
	}

	void SaveJPG(string filename, int width, int height, Color[] pixels)
	{
		using (FileStream fs = new FileStream(filename, FileMode.Create))
		{
			try
			{
				BinaryWriter bw = new BinaryWriter(fs);

				if ( bw != null )
				{
					Quality = Mathf.Clamp(Quality, 0.0f, 100.0f);
					JPGEncoder NewEncoder = new JPGEncoder(pixels, width, height, Quality);
					NewEncoder.doEncoding();
					byte[] TexData = NewEncoder.GetBytes();

					bw.Write(TexData);
					bw.Close();
				}
			}
			catch ( System.Exception ex )
			{
				Debug.Log("Error Writing File! " + ex);			
			}
			finally
			{
				fs.Close();
			}
		}
	}


	void UploadJPG(string filename, int width, int height, Color[] pixels)
	{
		Quality = Mathf.Clamp(Quality, 0.0f, 100.0f);
		JPGEncoder NewEncoder = new JPGEncoder(pixels, width, height, Quality);
		NewEncoder.doEncoding();
		byte[] TexData = NewEncoder.GetBytes();

		UploadFile(TexData, m_URL, filename);
	}

#if false
	void UploadTGA(string filename, int width, int height, byte[] pixels)
	{
		//byte[] data = new byte[output1.Length * 4];
		//Buffer.BlockCopy(output1, 0, data, 0, data.Length);

		UploadFile(pixels, m_URL, filename);
	}
#endif

	void SaveTGAOld(string filename, int width, int height, byte[] pixels, bool alpha)
	{
		FileStream fs = new FileStream(filename, FileMode.Create);

		if ( fs != null )
		{
			BinaryWriter bw = new BinaryWriter(fs);

			if ( bw != null )
			{
				bw.Write((short)0);
				bw.Write((byte)2);
				bw.Write((int)0);
				bw.Write((int)0);
				bw.Write((byte)0);
				bw.Write((short)width);
				bw.Write((short)height);
				if ( alpha )
				{
					bw.Write((byte)32);
					bw.Write((byte)0);
				}
				else
				{
					bw.Write((byte)24);
					bw.Write((byte)0);
				}

				for ( int h = 0; h < pixels.Length; h++ )
					bw.Write(pixels[h]);

				bw.Close();
			}

			fs.Close();
		}
	}

	void SaveTGA(string filename, int width, int height, byte[] pixels, bool alpha)
	{
		using (FileStream fs = new FileStream(filename, FileMode.Create))
		{
			try
			{
				BinaryWriter bw = new BinaryWriter(fs);

				if ( bw != null )
				{
					bw.Write((short)0);
					bw.Write((byte)2);
					bw.Write((int)0);
					bw.Write((int)0);
					bw.Write((byte)0);
					bw.Write((short)width);
					bw.Write((short)height);
					if ( alpha )
					{
						bw.Write((byte)32);
						bw.Write((byte)0);
					}
					else
					{
						bw.Write((byte)24);
						bw.Write((byte)0);
					}

					for ( int h = 0; h < pixels.Length; h++ )
						bw.Write(pixels[h]);

					bw.Close();
				}
			}
			catch (System.Exception ex)
			{
				Debug.Log("Error Writing File! " + ex);			
			}
			finally
			{
				fs.Close();
			}
		}
	}

	public void CalcUpscale()
	{
		float w = Width / ((float)Screen.width / (float)Dpi);	// * Width;
		ResUpscale = (int)(w);
		GrabWidthWillBe = Screen.width * ResUpscale;
		GrabHeightWillBe = Screen.height * ResUpscale;
	}

	public void CalcUpscale(int width, int height)
	{
		float w = Width / ((float)width / (float)Dpi);	// * Width;
		ResUpscale = (int)(w);
		GrabWidthWillBe = width * ResUpscale;
		GrabHeightWillBe = height * ResUpscale;
	}

	public void CalcEstimate()
	{
		NumberOfGrabs = ResUpscale * ResUpscale * AASamples;

		if ( UseDOF )
		{
			NumberOfGrabs *= totalSegments;
		}

		EstimatedTime = NumberOfGrabs * 0.41f;
	}

	IEnumerator GrabCoroutine()
	{
		yield return new WaitForEndOfFrame();

		if ( OutputFormat == IMGFormat.Tga )
			DoGrabTGA();
		else
			DoGrabJPG();

		if ( doingSeqGrab )
		{
			//grabTime += 1.0f / (float)framerate;
			gtime += 1.0f / (float)framerate;
			if ( gtime > grabTime )
			{
				doingSeqGrab = false;
				Debug.Log("Sequence complete");
			}

			framenumber++;

			Debug.Log("seq grab " + framenumber + " " + gtime);
		}
		yield return null;
	}

	void LateUpdate()
	{
		if ( grabFromStart )
		{
			DoGrab = true;
		}

		if ( doingSeqGrab )
		{
			if ( Input.GetKeyDown(CancelKey) || CancelSeqGrab )
			{
				CancelSeqGrab = false;
				doingSeqGrab = false;
			}
		}

		if ( DoGrab || Input.GetKeyDown(GrabKey) || doingSeqGrab )
		{
			DoGrab = false;

			if ( sequenceGrab && doingSeqGrab == false )
			{
				Time.captureFramerate = (int)framerate;
				doingSeqGrab = true;
				framenumber = 0;
				gtime = 0.0f;
			}
#if UNITY_IPHONE
			Path = Application.persistentDataPath + "/";
#endif
			//StartCoroutine(GrabCoroutine());

			if ( CalcFromSize )
				CalcUpscale();

			CalcEstimate();

			if ( UseCoroutine )
			{
				StartCoroutine(GrabCoroutine());
			}
			else
			{
				if ( OutputFormat == IMGFormat.Tga )
					DoGrabTGA();
				else
					DoGrabJPG();

				if ( doingSeqGrab )
				{
					gtime += 1.0f / (float)framerate;
					if ( gtime > grabTime )
					{
						doingSeqGrab = false;
						Debug.Log("Sequence complete");
					}

					framenumber++;

					Debug.Log("seq grab " + framenumber + " " + gtime);
				}
			}
		}
	}

	void OnDrawGizmos()
	{
		if ( CalcFromSize )
			CalcUpscale();

		CalcEstimate();
	}

	IEnumerator UploadFileCo(byte[] data, string uploadURL, string filename)
	{
		WWWForm postForm = new WWWForm();
		// version 1
		//postForm.AddBinaryData("theFile",localFile.bytes);
 
		//Debug.Log("uploading " + filename);
		// version 2
		postForm.AddField("action", "Upload Image");
		postForm.AddBinaryData("theFile", data, filename, "images/jpg");	//text/plain");
 
		//Debug.Log("url " + uploadURL);
		WWW upload = new WWW(uploadURL, postForm);
		yield return upload;
	}
 
	void UploadFile(byte[] data, string uploadURL, string filename)
	{
		//Debug.Log("Start upload");
		StartCoroutine(UploadLevel(data, uploadURL, filename));
		//Debug.Log("len " + data.Length);
	}

	IEnumerator UploadLevel(byte[] data, string uploadURL, string filename)
	{
		WWWForm form = new WWWForm();

		form.AddField("action", "level upload");
		form.AddField("file", "file");
		form.AddBinaryData("file", data, filename, "images/jpg");
		//Debug.Log("url " + uploadURL);

		WWW w = new WWW(uploadURL, form);
		yield return w;

		if ( w.error != null )
		{
			print("error");
			print(w.error);
		}
		else
		{
			if ( w.uploadProgress == 1 && w.isDone )
			{
				yield return new WaitForSeconds(5);
			}
		}
	}

#if false
	    <?
        if ( isset ($_POST['action']) ) {
            if($_POST['action'] == "Upload Image") {
                unset($imagename);
     
                if(!isset($_FILES) && isset($HTTP_POST_FILES)) $_FILES = $HTTP_POST_FILES;
     
                if(!isset($_FILES['fileUpload'])) $error["image_file"] = "An image was not found.";
     
                $imagename = basename($_FILES['fileUpload']['name']);
     
                if(empty($imagename)) $error["imagename"] = "The name of the image was not found.";
     
                if(empty($error)) {
                    $newimage = "images/" . $imagename;
                    //echo $newimage;
                    $result = @move_uploaded_file($_FILES['fileUpload']['tmp_name'], $newimage);
                    if ( empty($result) ) $error["result"] = "There was an error moving the uploaded file.";
                }
            }
        } else {
            echo "no form data found";
        }
    ?>
#endif
}
