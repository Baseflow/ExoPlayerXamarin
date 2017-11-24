// **********************************************************************
// 
//   TrackKey.cs
//   
//   This file is subject to the terms and conditions defined in
//   file 'LICENSE.txt', which is part of this source code package.
//   
//   Copyright (c) 2017, Sylvain Gravel
// 
// ***********************************************************************

using Java.Lang;

namespace Com.Google.Android.Exoplayer2.Source.Smoothstreaming.Manifest
{
    public partial class TrackKey
    {
        public int CompareTo(Object o)
        {
            return this.CompareTo(o as TrackKey);
        }
    }
}
