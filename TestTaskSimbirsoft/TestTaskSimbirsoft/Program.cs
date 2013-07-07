using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using TextHandlerInterface;

namespace TestTaskSimbirsoft
{
    class Program
    {     
        static void Main()
        {
            Console.Clear();
            var dirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Run(dirPath);
            Console.ReadKey();
        }

        /// <summary>
        /// Возвращает список (словарь) доступных в указанной директории обработчиков.
        /// </summary>
        /// <returns>Список (словарь) обработчиков.</returns>
        private static Dictionary<int, string> GetLibrariesInDirectory(string dirPath)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(dirPath))
                {
                    throw new ApplicationException("Путь к рабочей папке не найден!");
                }

                var libs = new Dictionary<int, string>();

                var pathesToLibs = Directory.GetFiles(dirPath, "*HandlerLibrary.dll");
                foreach (var path in pathesToLibs)
                {
                    libs.Add(libs.Count + 1, path);
                }

                return libs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Выводит на экран список доступных обработчиков.
        /// </summary>
        /// <param name="libs">Список (словарь) доступных обработчиков.</param>
        private static void PrintListOfLibs(Dictionary<int, string> libs)
        {
            try
            {
                Console.WriteLine("Для начала работы выберите из списка один из доступных обработчиков:");
                foreach (var lib in libs)
                {
                    var curLibName = lib.Value.Split(new[] { '\\' }).Last();
                    Console.WriteLine("{0} - работа с \"{1}\".", lib.Key, curLibName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Возвращает имя (путь) выбранного пользователем обработчика.
        /// </summary>
        /// <param name="libs">Список (словарь) доступных обработчиков.</param>
        /// <returns>Имя (путь) выбранного пользователем обработчика</returns>
        private static string SelectHandler(Dictionary<int, string> libs)
        {
            try
            {
                Console.Clear();
                string chooseLibName;
                while (true)
                {
                    PrintListOfLibs(libs);

                    var key = Console.ReadLine();
                    if (!String.IsNullOrWhiteSpace(key))
                    {
                        if (libs.TryGetValue(Int32.Parse(key), out chooseLibName))
                        {
                            break;
                        }
                    }
                    Console.Clear();
                    Console.WriteLine("Неверное значение!");
                }
                return chooseLibName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Загружает выбранный обработчик и передает ему список имен входных и выходных файлов для обработки.
        /// </summary>
        /// <param name="handlerLibName">Имя обработчика.</param>
        /// <param name="dictFileName">Имя (путь) файла словаря (без указания расширения файла).</param>
        /// <param name="textFileName">Имя (путь) входного файла с текстом (без указания расширения файла).</param>
        /// <param name="resultFileName">Имя (путь) результирующего файла (без указания расширения файла).</param>
        private static void LoadHandler(string handlerLibName, string dictFileName, string textFileName, string resultFileName)
        {
            try
            {
                if (!File.Exists(handlerLibName))
                {
                    throw new ApplicationException("Библиотека обработчика " + handlerLibName + " не найдена!");
                }

                var handlerAssembly = Assembly.LoadFrom(handlerLibName);
                foreach (var t in handlerAssembly.GetExportedTypes())
                {
                    if (t.IsClass && typeof(IHandler).IsAssignableFrom(t))
                    {
                        var handler = (IHandler)Activator.CreateInstance(t);
                        handler.Run(dictFileName, textFileName, resultFileName);
                        break;
                    }
                }
                Console.WriteLine("Готово!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Возвращает имя (путь) рабочего файла.
        /// Метод используется для опроса пользователя и получения имени (пути) файла, с которым следует работать программе. 
        /// </summary>
        /// <param name="message">Сообщение, которое следует отобразить пользователю. Может содержать явное указание, какой именно файл (словарь, исходный текст, результирующий файл) требуется программе.</param>
        /// <returns>Имя (путь) рабочего файла.</returns>
        protected static string GetFileName(string message)
        {
            string fileName;
            while (true)
            {
                Console.WriteLine(message);
                fileName = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Неверное имя файла!");
                    continue;
                }

                if (!fileName.Contains("."))
                {
                    break;
                }
            }
            return fileName;
        }

        /// <summary>
        /// Запуск обработки с поиском доступных программе (в директории программы) обработчиков.
        /// </summary>
        private static void Run(string dirPath)
        {
            try
            {
                var libs = GetLibrariesInDirectory(dirPath);
                if (libs == null)
                {
                    throw new ApplicationException("Не удалось найти ни одного обработчика!");
                }

                var dictFileName = GetFileName("Путь к файлу словаря не задан или задан неверно!\nВведите путь к файлу словаря (расширение файла указываеть НЕ нужно): ");
                var textFileName = GetFileName("Путь к файлу с текстом не задан или задан неверно!\nВведите путь к файлу с текстом (расширение файла указываеть НЕ нужно): ");
                var resultFileName = GetFileName("Путь к результирующему файлу не задан или задан неверно!\nВведите путь к результирующему файлу (расширение файла указываеть НЕ нужно): ");

                var chooseLibName = SelectHandler(libs);
                if (String.IsNullOrWhiteSpace(chooseLibName))
                {
                    throw new ApplicationException("Несуществующий путь к обработчику!");
                }
                LoadHandler(chooseLibName, dictFileName, textFileName, resultFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
