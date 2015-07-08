﻿// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Configuration
{
    public class Ontology : ConfigurationElement
    {
        [ConfigurationProperty("Prefix")]
        public string Prefix
        {
            get { return (string)base["Prefix"]; }
            set { base["Prefix"] = value; }
        }

        public UriRef Uri
        {
            get
            {
                if (string.IsNullOrEmpty(uriString))
                    return null;
                return new UriRef(uriString);
            }
            set
            {
                uriString = value.OriginalString;
            }
        }

        [ConfigurationProperty("Uri", IsKey = true, IsRequired = true)]
        public string uriString
        {
            get { return (string)base["Uri"]; }
            set { base["Uri"] = value; }
        }

        public object KeyElement
        {
            get { return uriString; }
        }

        [ConfigurationProperty("FileSource", IsRequired = true)]
        public FileSource FileSource
        {
            get { return (FileSource)base["FileSource"]; }
            set { base["FileSource"] = value; }
        }

        public UriRef MetadataUri
        {
            get
            {
                if (string.IsNullOrEmpty(metadataUriString))
                    return null;
                return new UriRef(metadataUriString);
            }
            set
            {
                metadataUriString = value.OriginalString;
            }
        }

        [ConfigurationProperty("MetadataUri", IsRequired = false)]
        public string metadataUriString
        {
            get { return (string)base["MetadataUri"]; }
            set { base["MetadataUri"] = value; }
        }

        public override string ToString()
        {
            if (Uri != null)
                return Uri.OriginalString;
            else
                return "Empty Ontology";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

}

}