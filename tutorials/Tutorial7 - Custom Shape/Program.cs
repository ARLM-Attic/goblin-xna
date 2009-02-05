using System;

namespace Tutorial7___Custom_Shape
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Tutorial7 game = new Tutorial7())
            {
                game.Run();
            }
        }
    }
}

