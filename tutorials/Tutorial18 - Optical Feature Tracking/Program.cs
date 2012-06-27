using System;

namespace Tutorial18___Optical_Feature_Tracking
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Tutorial18 game = new Tutorial18())
            {
                game.Run();
            }
        }
    }
#endif
}

