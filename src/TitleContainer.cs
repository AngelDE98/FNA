#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region ENABLE_ZIPPING Option
//#define ENABLE_ZIPPING
/* ZipArchive is only available in .NET Framework 4.5+ and is
 * only beneficial for Android when packaging your content in OBBs.
 * Using this also requires the FNA_CONTENT_FORCE_ZIP environment
 * flag to be set, which is automatically done by the unofficial
 * FNADroid wrapper.
 *
 * As it's only available in .NET 4.5+, you also need to
 * set the build target of FNA to .NET 4.5+, killing support for
 * MonoKickstart (Linux, OSX) until it's updated.
 *
 * Finally, using FNA_CONTENT_FORCE_ZIP may break your game in
 * unexpected ways.
 *
 * You normally won't need this.
 * -ade
 */
#endregion

#region ENABLE_ZIPPING_REFLECTION Option
#define ENABLE_ZIPPING_REFLECTION
/* Unwanted cousin of ENABLE_ZIPPING, using reflection.
 * It does not require you to set the build target to .NET 4.5+,
 * but it still only works on .NET 4.5+ (duh).
 *
 * As it uses reflection, performance is going to take a hit.
 *
 * You NEVER need this. Whoever thinks that they need this should use
 * the above or should accept the performance issues.
 * -ade
 */
#endregion

#region Using Statements
using System;
using System.IO;

#if ENABLE_ZIPPING
using System.IO.Compression;
#elif ENABLE_ZIPPING_REFLECTION
using System.Reflection;
#endif

using Microsoft.Xna.Framework.Utilities;
#endregion

namespace Microsoft.Xna.Framework
{
	public static class TitleContainer
	{
		#region Internal Static Properties

		static internal string Location
		{
			get;
			private set;
		}
        
#if ENABLE_ZIPPING
        static internal ZipArchive Zip
		{
			get;
			private set;
		}
#else
        //FNA should still be able to set it to null and
        //_REFLECTION needs to access this. To my knowledge,
        //it's required to remove all references to the
        //ZIP to allow the GC to call Finalize().
        //-ade 
        static internal object Zip
		{
			get;
			private set;
		}
#endif

		#endregion
        
#if ENABLE_ZIPPING_REFLECTION
        #region Internal Reflection-related Variables
        
        internal static MethodInfo m_ZipArchive_GetEntry;
        internal static object[] arg_ZipArchive_GetEntry = new object[1];
        internal static MethodInfo m_ZipArchiveEntry_Open;
        internal static object[] arg_ZipArchiveEntry_Open = new object[0];
        
        #endregion
#endif

		#region Static Constructor

		static TitleContainer()
		{
			Location = AppDomain.CurrentDomain.BaseDirectory;
            
#if ENABLE_ZIPPING || ENABLE_ZIPPING_REFLECTION
            string forcedZip = Environment.GetEnvironmentVariable("FNA_CONTENT_FORCE_ZIP");
            if (!string.IsNullOrEmpty(forcedZip)) {
                Location = "";
                #endif
                #if ENABLE_ZIPPING
                //4.5+
                Zip = newZipArchive(File.OpenRead(forcedZip), ZipArchiveMode.Read);
                #else
                //4.5+ with 4.0 as build target because reasons
                //Go through all assemblies in the AppDomain and hope that System.IO.Compression has been loaded
                Assembly a_System_IO_Compression = null;
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in asms) {
                    if (asm.GetName().Name == "System.IO.Compression") {
                        a_System_IO_Compression = asm;
                        break;
                    }
                }
                //Assembly not found? Time to use Assembly.Load
                if (a_System_IO_Compression == null) {
                    a_System_IO_Compression = Assembly.Load("System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                    //If it's not found, don't worry: The game will just crash telling you about it.
                }
                if (a_System_IO_Compression == null) {
                    //If it's not found and the game didn't crash, screw this
                    Console.WriteLine("FNA_CONTENT_FORCE_ZIP: Assembly not found.");
                    return;
                }
                //Get the types and methods
                Type t_ZipArchive = a_System_IO_Compression.GetType("System.IO.Compression.ZipArchive");
                if (t_ZipArchive == null) {
                    Console.WriteLine("FNA_CONTENT_FORCE_ZIP: ZipArchive not found.");
                    return;
                }
                Type t_ZipArchiveEntry = a_System_IO_Compression.GetType("System.IO.Compression.ZipArchiveEntry");
                if (t_ZipArchiveEntry == null) {
                    Console.WriteLine("FNA_CONTENT_FORCE_ZIP: ZipArchiveEntry not found.");
                    return;
                }
                m_ZipArchive_GetEntry = t_ZipArchive.GetMethod("GetEntry", new Type[] {typeof(string)});
                if (m_ZipArchive_GetEntry == null) {
                    Console.WriteLine("FNA_CONTENT_FORCE_ZIP: ZipArchive.GetEntry not found.");
                    return;
                }
                m_ZipArchiveEntry_Open = t_ZipArchiveEntry.GetMethod("Open", new Type[0]);
                if (m_ZipArchive_GetEntry == null) {
                    Console.WriteLine("FNA_CONTENT_FORCE_ZIP: ZipArchiveEntry.Open not found.");
                    return;
                }
                //FINALLY call the constructor
                //As the doc says "The stream that contains the archive to be read", we simply skip the ZipArchiveMode
                Zip = t_ZipArchive.GetConstructor(new Type[] {typeof(Stream)}).Invoke(new object[] {File.OpenRead(forcedZip)});
                #endif
                #if ENABLE_ZIPPING || ENABLE_ZIPPING_REFLECTION
            }
#endif
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Returns an open stream to an exsiting file in the title storage area.
		/// </summary>
		/// <param name="name">The filepath relative to the title storage area.</param>
		/// <returns>A open stream or null if the file is not found.</returns>
		public static Stream OpenStream(string name)
		{
			string safeName = FileHelpers.NormalizeFilePathSeparators(name);

			if (Path.IsPathRooted(safeName))
			{
				return File.OpenRead(safeName);
			}
            
#if ENABLE_ZIPPING || ENABLE_ZIPPING_REFLECTION
            if (Zip != null)
            {
                System.Console.WriteLine("TitleContainer.OpenStream name: " + name);
                #endif
                #if ENABLE_ZIPPING
                //4.5+
                System.Console.WriteLine("TitleContainer.OpenStream entry: " + Zip.GetEntry(safeName));
                return Zip.GetEntry(safeName).Open();
                #else
                //4.5+ with 4.0 as build target because reasons
                arg_ZipArchive_GetEntry[0] = safeName;
                object o_ZipArchiveEntry = m_ZipArchive_GetEntry.Invoke(Zip, arg_ZipArchive_GetEntry);
                System.Console.WriteLine("TitleContainer.OpenStream entry: " + o_ZipArchiveEntry);
                return (Stream) m_ZipArchiveEntry_Open.Invoke(o_ZipArchiveEntry, arg_ZipArchiveEntry_Open);
                #endif
                #if ENABLE_ZIPPING || ENABLE_ZIPPING_REFLECTION
            }
#endif
            
			return File.OpenRead(Path.Combine(Location, safeName));
		}

		#endregion
	}
}

