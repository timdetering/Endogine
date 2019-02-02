//**********************************************
// Project: Flood Fill Algorithms in C# & GDI+
// File Description: Flood Fill Class
//
// Copyright: Copyright 2003 by Justin Dunlap.
//    Any code herein can be used freely in your own 
//    applications, provided that:
//     * You agree that I am NOT to be held liable for
//       any damages caused by this code or its use.
//     * You give proper credit to me if you re-publish
//       this code.
//**********************************************
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;

namespace Endogine.BitmapHelpers.FloodFill
{
	using System;

	/// <summary>
	/// TODO - Add class summary
	/// </summary>
	/// <remarks>
	/// 	created by - J Dunlap
	/// 	created on - 7/2/2003 11:44:33 PM
	/// </remarks>
	public class FloodFiller : AbstractFloodFiller 
	{	
		private int m_fillcolor=255<<8;
		private bool m_bActuallyFill = true;
		//private bool[,] m_filled;
		
		/// <summary>
		/// Default constructor - initializes all fields to default values
		/// </summary>
		public FloodFiller() 
		{
		}

		public ArrayList GetFloodArray(Bitmap bmp, Point pt)
		{
			//get the bits
			BitmapData bmpData=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
			System.IntPtr Scan0 = bmpData.Scan0;
		    
			m_bActuallyFill = true;
			//m_filled = new bool[bmpData.Width+1,bmpData.Height+1];

			unsafe
			{
				//resolve pointer
				byte * scan0=(byte *)(void *)Scan0;
				//get the starting color
				//[loc += Y offset + X offset]
				int loc=CoordsToIndex(pt.X,pt.Y,bmpData.Stride);//((bmpData.Stride*(pt.Y-1))+(pt.X*4));
				int color= *((int*)(scan0+loc));
		    	
				//create the array of bools that indicates whether each pixel
				//has been checked.  (Should be bitfield, but C# doesn't support bitfields.)
				PixelsChecked=new bool[bmpData.Width+1,bmpData.Height+1];
		    	
				if(m_FillDiagonal)
					LinearFloodFill8(scan0,pt.X,pt.Y,new Size(bmpData.Width,bmpData.Height),bmpData.Stride,(byte*)&color);
				else
					LinearFloodFill4(scan0,pt.X,pt.Y,new Size(bmpData.Width,bmpData.Height),bmpData.Stride,(byte*)&color);
			}
		    
			bmp.UnlockBits(bmpData);


			ArrayList aPoints = new ArrayList();
			for (int x=0; x<bmpData.Width+1; x++)
			{
				for (int y=0; y<bmpData.Height+1; y++)
				{
					if (PixelsChecked[x,y])
						aPoints.Add(new Point(x,y));
				}
			}
			return aPoints;
		}

		///<summary>initializes the FloodFill operation</summary>
		public void FloodFill(Bitmap bmp, Point pt)
		{
			//int ctr=timeGetTime();
			
			//get the color's int value, and convert it from RGBA to BGRA format (as GDI+ uses BGRA)
			m_fillcolor=ColorTranslator.ToWin32(m_fillcolorcolor);
			m_fillcolor=BGRA(GetB(m_fillcolor),GetG(m_fillcolor),GetR(m_fillcolor),GetA(m_fillcolor));

			//get the bits
			BitmapData bmpData=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
			System.IntPtr Scan0 = bmpData.Scan0;
		    
			unsafe
			{
				//resolve pointer
				byte * scan0=(byte *)(void *)Scan0;
				//get the starting color
				//[loc += Y offset + X offset]
				int loc=CoordsToIndex(pt.X,pt.Y,bmpData.Stride);//((bmpData.Stride*(pt.Y-1))+(pt.X*4));
				int color= *((int*)(scan0+loc));
		    	
				//create the array of bools that indicates whether each pixel
				//has been checked.  (Should be bitfield, but C# doesn't support bitfields.)
				PixelsChecked=new bool[bmpData.Width+1,bmpData.Height+1];
		    	
				if(m_FillDiagonal)
					LinearFloodFill8(scan0,pt.X,pt.Y,new Size(bmpData.Width,bmpData.Height),bmpData.Stride,(byte*)&color);
				else
					LinearFloodFill4(scan0,pt.X,pt.Y,new Size(bmpData.Width,bmpData.Height),bmpData.Stride,(byte*)&color);
			}
		    
			bmp.UnlockBits(bmpData);
			
			//m_TimeBenchmark=timeGetTime()-ctr;
			
		}
		
		//***********
		//LINEAR ALGORITHM
		//***********
		
		unsafe void LinearFloodFill4( byte* scan0, int x, int y,Size bmpsize, int stride, byte* startcolor)
		{
			//offset the pointer to the point passed in
			int* p=(int*) (scan0+(CoordsToIndex(x,y, stride)));
				
			//FIND LEFT EDGE OF COLOR AREA
			int LFillLoc=x; //the location to check/fill on the left
			int* ptr=p; //the pointer to the current location
			while(true)
			{
				if (m_bActuallyFill)
					ptr[0]=m_fillcolor;	//fill with the color

				PixelsChecked[LFillLoc,y]=true;
				LFillLoc--;				//de-increment counter
				ptr-=1;					//de-increment pointer
				if(LFillLoc<=0 || (PixelsChecked[LFillLoc,y]) || !CheckPixel((byte*)ptr,startcolor))
					break;				//exit loop if we're at edge of bitmap or color area
			}
			LFillLoc++;

			//FIND RIGHT EDGE OF COLOR AREA
			int RFillLoc=x; //the location to check/fill on the left
			ptr=p;
			while(true)
			{
				if (m_bActuallyFill)
					ptr[0]=m_fillcolor; //fill with the color
				PixelsChecked[RFillLoc,y]=true;
				RFillLoc++; 		 //increment counter
				ptr+=1;				 //increment pointer
				if(RFillLoc>=bmpsize.Width || (PixelsChecked[RFillLoc,y]) || !CheckPixel((byte*)ptr,startcolor))
					break;			 //exit loop if we're at edge of bitmap or color area
				
			}
			RFillLoc--;
			
			
			//START THE LOOP UPWARDS AND DOWNWARDS			
			ptr=(int*)(scan0+CoordsToIndex(LFillLoc,y,stride));
			for(int i=LFillLoc;i<=RFillLoc;i++)
			{
				//START LOOP UPWARDS
				//if we're not above the top of the bitmap and the pixel above this one is within the color tolerance
				if(y>0 && CheckPixel((byte*)(scan0+CoordsToIndex(i,y-1,stride)),startcolor) && (!(PixelsChecked[i,y-1])))
					LinearFloodFill4(scan0, i,y-1,bmpsize,stride,startcolor);
				//START LOOP DOWNWARDS
				if(y<(bmpsize.Height-1) && CheckPixel((byte*)(scan0+CoordsToIndex(i,y+1,stride)),startcolor) && (!(PixelsChecked[i,y+1])))
					LinearFloodFill4(scan0, i,y+1,bmpsize,stride,startcolor);
				ptr+=1;
			}
		}
		
		unsafe void LinearFloodFill8( byte* scan0, int x, int y,Size bmpsize, int stride, byte* startcolor)
		{
			
			//offset the pointer to the point passed in
			int* p=(int*) (scan0+(CoordsToIndex(x,y, stride)));
			
			
			//FIND LEFT EDGE OF COLOR AREA
			int LFillLoc=x; //the location to check/fill on the left
			int* ptr=p; //the pointer to the current location
			while(true)
			{
				if (m_bActuallyFill)
					ptr[0]=m_fillcolor; 	 //fill with the color
				PixelsChecked[LFillLoc,y]=true;
				LFillLoc--; 		 	 //de-increment counter
				ptr-=1;				 	 //de-increment pointer
				if(LFillLoc<=0 || !CheckPixel((byte*)ptr,startcolor) ||  (PixelsChecked[LFillLoc,y]))
					break;			 	 //exit loop if we're at edge of bitmap or color area
				
			}
			LFillLoc++;
			
			//FIND RIGHT EDGE OF COLOR AREA
			int RFillLoc=x; //the location to check/fill on the left
			ptr=p;
			while(true)
			{
				if (m_bActuallyFill)
					ptr[0]=m_fillcolor; //fill with the color
				PixelsChecked[RFillLoc,y]=true;
				RFillLoc++; 		 //increment counter
				ptr+=1;				 //increment pointer
				if(RFillLoc>=bmpsize.Width || !CheckPixel((byte*)ptr,startcolor) ||  (PixelsChecked[RFillLoc,y]))
					break;			 //exit loop if we're at edge of bitmap or color area
				
			}
			RFillLoc--;
			
			
			//START THE LOOP UPWARDS AND DOWNWARDS			
			ptr=(int*)(scan0+CoordsToIndex(LFillLoc,y,stride));
			for(int i=LFillLoc;i<=RFillLoc;i++)
			{
				//START LOOP UPWARDS
				//if we're not above the top of the bitmap and the pixel above this one is within the color tolerance
				//START LOOP DOWNWARDS
				if(y>0)
				{
					//UP
					if(CheckPixel((byte*)(scan0+CoordsToIndex(i,y-1,stride)),startcolor) && (!(PixelsChecked[i,y-1])))
						LinearFloodFill8(scan0, i,y-1,bmpsize,stride,startcolor);
					//UP-LEFT
					if(x>0 && CheckPixel((byte*)(scan0+CoordsToIndex(i-1,y-1,stride)),startcolor) && (!(PixelsChecked[i-1,y-1])))
						LinearFloodFill8(scan0, i-1,y-1,bmpsize,stride,startcolor);
					//UP-RIGHT
					if(x<(bmpsize.Width-1) && CheckPixel((byte*)(scan0+CoordsToIndex(i+1,y-1,stride)),startcolor) && (!(PixelsChecked[i+1,y-1])))
						LinearFloodFill8(scan0, i+1,y-1,bmpsize,stride,startcolor);
				}
				
				if(y<(bmpsize.Height-1)) 
				{
					//DOWN
					if(CheckPixel((byte*)(scan0+CoordsToIndex(i,y+1,stride)),startcolor) && (!(PixelsChecked[i,y+1])))
						LinearFloodFill8(scan0, i,y+1,bmpsize,stride,startcolor);
					//DOWN-LEFT
					if(x>0 && CheckPixel((byte*)(scan0+CoordsToIndex(i-1,y+1,stride)),startcolor) && (!(PixelsChecked[i-1,y+1])))
						LinearFloodFill8(scan0, i-1,y+1,bmpsize,stride,startcolor);
					//UP-RIGHT
					if(x<(bmpsize.Width-1) && CheckPixel((byte*)(scan0+CoordsToIndex(i+1,y+1,stride)),startcolor) && (!(PixelsChecked[i+1,y+1])))
						LinearFloodFill8(scan0, i+1,y+1,bmpsize,stride,startcolor);
					
				}
				
				ptr+=1;
			}
		}

		//*********
		//HELPER FUNCTIONS
		//*********
		
		///<summary>Sees if a pixel is within the color tolerance range.</summary>
		//px - a pointer to the pixel to check
		//startcolor - a pointer to the color of the pixel we started at
		unsafe bool CheckPixel(byte* px, byte* startcolor)
		{
			bool ret=true;
			for(byte i=0;i<3;i++)
				ret&= (px[i]>= (startcolor[i]-m_Tolerance[i])) && px[i] <= (startcolor[i]+m_Tolerance[i]);		    
			return ret;
		}
		
		///<summary>Given X and Y coordinates and the bitmap's stride, returns a pointer offset</summary>
		int CoordsToIndex(int x, int y, int stride)
		{
			return (stride*y)+(x*4);
		}		
	}
}
