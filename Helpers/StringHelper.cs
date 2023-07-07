namespace FileStorageApi.Helpers
{
        public static class StringHelper
        {
                #region CONSTANTS

                /// <summary>
                /// Possible chars for generating random strings
                /// </summary>
                private const string ConstPossibleRandomChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                #endregion

                #region PUBLIC METHODS

                /// <summary>
                /// Generates random string of the specified length.
                /// Can be used as password etc.
                /// </summary>
                /// <param name="length"></param>
                /// <returns></returns>
                public static string GenerateRandomString(int length)
                {
                        var random = new Random();

                        return new string(
                            Enumerable.Repeat(ConstPossibleRandomChars, length)
                                      .Select(s => s[random.Next(s.Length)])
                                      .ToArray());
                }

                #endregion
        }
}
