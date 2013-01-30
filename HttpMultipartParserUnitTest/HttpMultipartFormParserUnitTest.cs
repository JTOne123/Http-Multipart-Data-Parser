﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpMultipartFormParserUnitTest.cs" company="Jake Woods">
//   Copyright (c) 2013 Jake Woods
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
//   and associated documentation files (the "Software"), to deal in the Software without restriction, 
//   including without limitation the rights to use, copy, modify, merge, publish, distribute, 
//   sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
//   is furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all copies 
//   or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//   INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
//   PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR 
//   ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <author>Jake Woods</author>
// <summary>
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HttpMultipartParserUnitTest
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using HttpMultipartParser;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     The http multipart form parser unit test.
    /// </summary>
    [TestClass]
    public class HttpMultipartFormParserUnitTest
    {
        #region Static Fields

        /// <summary>
        ///     Raw multipart/form-data for the
        ///     <see cref="TestData">
        ///         <c>ObjNamehEre</c>
        ///     </see>
        ///     test object.
        /// </summary>
        private static readonly string MultipleDataAndParams = TestUtil.TrimAllLines(@"--boundry
              Content-Disposition: form-data; name=""text""
              
              textdata
              --boundry
              Content-Disposition: form-data; name=""after""
              
              afterdata
              --boundry
              Content-Disposition: form-data; name=""file""; filename=""data.txt""
              Content-Type: text/plain

              I am the first data 
              --boundry
              Content-Disposition: form-data; name=""newfile""; filename=""superdata.txt""
              Content-Type: text/plain

              I am the second data
              --boundry
              Content-Disposition: form-data; name=""never""

              neverdata 
              --boundry
              Content-Disposition: form-data; name=""waylater""

              waylaterdata 
              --boundry--");

        /// <summary>
        ///     Test case for multiple parameters and files.
        /// </summary>
        private static readonly TestData MultipleDataAndParamsTestData = new TestData(
            MultipleDataAndParams, 
            new Dictionary<string, ParameterPart>
                {
                    { "text", new ParameterPart("text", "textdata") }, 
                    { "after", new ParameterPart("after", "afterdata") }, 
                    { "never", new ParameterPart("never", "neverdata") }, 
                    { "waylater", new ParameterPart("waylater", "waylaterdata") }, 
                }, 
            new Dictionary<string, FilePart>
                {
                    {
                        "file", 
                        new FilePart(
                        "file", 
                        "data.txt", 
                        TestUtil.StringToStreamNoBom("I am the first data"))
                    }, 
                    {
                        "newfile", 
                        new FilePart(
                        "newfile", 
                        "superdata.txt", 
                        TestUtil.StringToStreamNoBom("I am the second data"))
                    }
                });

        /// <summary>
        ///     The small request.
        /// </summary>
        private static readonly string SmallRequest =
            TestUtil.TrimAllLines(@"-----------------------------265001916915724
                Content-Disposition: form-data; name=""textdata""
                
                Testdata
                -----------------------------265001916915724
                Content-Disposition: form-data; name=""file""; filename=""data.txt""
                Content-Type: text/plain

                This is a small file
                -----------------------------265001916915724
                Content-Disposition: form-data; name=""submit""

                Submit
                -----------------------------265001916915724--");

        /// <summary>
        ///     The small data test case with expected outcomes
        /// </summary>
        private static readonly TestData SmallTestData = new TestData(
            SmallRequest, 
            new Dictionary<string, ParameterPart>
                {
                    { "textdata", new ParameterPart("textdata", "Testdata") }, 
                    { "submit", new ParameterPart("submit", "Submit") }
                }, 
            new Dictionary<string, FilePart>
                {
                    {
                        "file", 
                        new FilePart(
                        "file", 
                        "data.txt", 
                        TestUtil.StringToStreamNoBom("This is a small file"))
                    }
                });

        /// <summary>
        ///     Raw multipart/form-data for the <see cref="TestData" /> object.
        /// </summary>
        private static readonly string TinyRequest = TestUtil.TrimAllLines(@"--boundry
            Content-Disposition: form-data; name=""text""

            textdata
            --boundry
            Content-Disposition: form-data; name=""file""; filename=""data.txt""
            Content-Type: text/plain

            tiny
            --boundry
            Content-Disposition: form-data; name=""after""

            afterdata
            --boundry--");

        /// <summary>
        ///     The tiny data test case with expected outcomes
        /// </summary>
        private static readonly TestData TinyTestData = new TestData(
            TinyRequest, 
            new Dictionary<string, ParameterPart>
                {
                    { "text", new ParameterPart("text", "textdata") }, 
                    { "after", new ParameterPart("after", "afterdata") }, 
                }, 
            new Dictionary<string, FilePart>
                {
                    {
                        "file", 
                        new FilePart(
                        "file", "data.txt", TestUtil.StringToStreamNoBom("tiny"))
                    }
                });

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Tests for correct detection of the boundary in the input stream.
        /// </summary>
        [TestMethod]
        public void CanAutoDetectBoundary()
        {
            using (Stream stream = TestUtil.StringToStream(TinyTestData.Request, Encoding.UTF8))
            {
                var parser = new MultipartFormDataParser(stream);
                Assert.IsTrue(TinyTestData.Validate(parser));
            }
        }

        /// <summary>
        ///     Ensures that boundary detection works even when the boundary spans
        ///     two different buffers.
        /// </summary>
        [TestMethod]
        public void CanDetectBoundariesCrossBuffer()
        {
            using (Stream stream = TestUtil.StringToStream(TinyTestData.Request, Encoding.UTF8))
            {
                var parser = new MultipartFormDataParser(stream, "boundry", Encoding.UTF8, 16);
                Assert.IsTrue(TinyTestData.Validate(parser));
            }
        }

        /// <summary>
        ///     The correctly handle mixed newline formats.
        /// </summary>
        [TestMethod]
        public void CorrectlyHandleMixedNewlineFormats()
        {
        }

        /// <summary>
        ///     Tests for correct handling of <c>crlf (\r\n)</c> in the input stream.
        /// </summary>
        [TestMethod]
        public void CorrectlyHandlesCRLF()
        {
            string request = TinyTestData.Request.Replace("\n", "\r\n");
            using (Stream stream = TestUtil.StringToStream(request, Encoding.UTF8))
            {
                var parser = new MultipartFormDataParser(stream, "boundry", Encoding.UTF8);
                Assert.IsTrue(TinyTestData.Validate(parser));
            }
        }

        /// <summary>
        ///     Initializes the test data before each run, this primarily
        ///     consists of resetting data stream positions.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            var testData = new[] { TinyTestData, SmallTestData, MultipleDataAndParamsTestData };
            foreach (TestData data in testData)
            {
                foreach (var pair in data.ExpectedFileData)
                {
                    pair.Value.Data.Position = 0;
                }
            }
        }

        /// <summary>
        ///     Checks that multiple files don't get in the way of parsing each other
        ///     and that everything parses correctly.
        /// </summary>
        [TestMethod]
        public void MultipleFilesAndParamsTest()
        {
            using (Stream stream = TestUtil.StringToStream(MultipleDataAndParamsTestData.Request, Encoding.UTF8))
            {
                var parser = new MultipartFormDataParser(stream, "boundry", Encoding.UTF8, 16);
                Assert.IsTrue(MultipleDataAndParamsTestData.Validate(parser));
            }
        }

        /// <summary>
        ///     The small data test.
        /// </summary>
        [TestMethod]
        public void SmallDataTest()
        {
            using (Stream stream = TestUtil.StringToStream(SmallTestData.Request))
            {
                // The boundry is missing the first two -- in accordance with the multipart
                // spec. (A -- is added by the parser, this boundry is what would be sent in the
                // requset header)
                var parser = new MultipartFormDataParser(stream, "---------------------------265001916915724");
                Assert.IsTrue(SmallTestData.Validate(parser));

            }
        }

        /// <summary>
        ///     The tiny data test.
        /// </summary>
        [TestMethod]
        public void TinyDataTest()
        {
            using (Stream stream = TestUtil.StringToStream(TinyTestData.Request, Encoding.UTF8))
            {
                var parser = new MultipartFormDataParser(stream, "boundry", Encoding.UTF8);
                TinyTestData.Validate(parser);
            }
        }

        #endregion

        /// <summary>
        ///     Represents a single parsing test case and the expected parameter/file outputs
        ///     for a given request.
        /// </summary>
        private class TestData
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TestData"/> class.
            /// </summary>
            /// <param name="request">
            /// The request.
            /// </param>
            /// <param name="expectedParams">
            /// The expected params.
            /// </param>
            /// <param name="expectedFileData">
            /// The expected file data.
            /// </param>
            public TestData(
                string request, 
                IDictionary<string, ParameterPart> expectedParams, 
                IDictionary<string, FilePart> expectedFileData)
            {
                this.Request = request;
                this.ExpectedParams = expectedParams;
                this.ExpectedFileData = expectedFileData;
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the expected file data.
            /// </summary>
            public IDictionary<string, FilePart> ExpectedFileData { get; set; }

            /// <summary>
            ///     Gets or sets the expected parameters.
            /// </summary>
            public IDictionary<string, ParameterPart> ExpectedParams { get; set; }

            /// <summary>
            ///     Gets or sets the request.
            /// </summary>
            public string Request { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Validates the output of the parser against the expected outputs for 
            /// this test
            /// </summary>
            /// <param name="parser">
            /// The parser to validate.
            /// </param>
            /// <returns>
            /// The <see cref="bool"/> representing if this test passed.
            /// </returns>
            public bool Validate(MultipartFormDataParser parser)
            {
                // Validate parameters
                foreach (var pair in this.ExpectedParams)
                {
                    if (!parser.Parameters.ContainsKey(pair.Key))
                    {
                        return false;
                    }

                    ParameterPart expectedPart = pair.Value;
                    ParameterPart actualPart = parser.Parameters[pair.Key];

                    if (expectedPart.Name != actualPart.Name || expectedPart.Data != actualPart.Data)
                    {
                        return false;
                    }
                }

                // Validate files
                foreach (var pair in this.ExpectedFileData)
                {
                    if (parser.Files[pair.Key] != pair.Value)
                    {
                        if (!parser.Files.ContainsKey(pair.Key))
                        {
                            return false;
                        }

                        FilePart expectedFile = pair.Value;
                        FilePart actualFile = parser.Files[pair.Key];

                        if (expectedFile.Name != actualFile.Name || expectedFile.FileName != actualFile.FileName)
                        {
                            return false;
                        }

                        // Read the data from the files and see if it's the same
                        var reader = new StreamReader(expectedFile.Data);
                        string expectedFileData = reader.ReadToEnd();

                        reader = new StreamReader(actualFile.Data);
                        string actualFileData = reader.ReadToEnd();

                        if (expectedFileData != actualFileData)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            #endregion
        }
    }
}