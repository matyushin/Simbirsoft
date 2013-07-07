using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TextHandlerInterface
{
    /// <summary>
    /// Интерфейс обработчика.
    /// Описывает один метод - запуск обработки (например, входного текстового файла).
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Метод запуска обработки (например, входного текстового файла).
        /// </summary>
        void Run(string dictFileName, string textFileName, string resultFileName);
    };

    /// <summary>
    /// Абстрактный класс, наследующий интерфейс IHandler.
    /// Выступает как базовый для различных реализаций интерфейса обработчика.
    /// </summary>
    public abstract class HandlerLibrary : IHandler
    {
        /// <summary>
        /// Указывает максимальное количество строк, которые можно записать в выходной файл.
        /// При превышении порога, указанного в этой константе, должен формироваться новый выходной файл.
        /// </summary>
        protected const int MaxLinesCount = 500;

        /// <summary>
        /// Абстрактный метод, реализующий метод Run() интерфейса обработчика IHandler.
        /// Используется для перегрузки метода в классах-наследниках.
        /// </summary>
        public abstract void Run(string dictFileName, string textFileName, string resultFileName);

        /// <summary>
        /// Метод используется для получения входной последовательности символов из указанного входного файла. Возвращает перечислитель, выполняющий итерацию считывания блока символов из входного файла.
        /// </summary>
        /// <param name="fileName">Имя входного файла с текстом.</param>
        /// <returns>Перечислитель, по которому можно получить каждый следующий блок символов из входного потока (файла).</returns>
        protected IEnumerable<string> GetInputTextFileContent(string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fileStream.Length > 2097152)
                {
                    throw new ApplicationException("Размер файла текста превышает максимальный допустимый размер!");
                }
                using (var inputStream = new StreamReader(fileStream, Encoding.GetEncoding("windows-1251")))
                {
                    var readBlockSize = 500;

                    while (true)
                    {
                        var resArray = new char[500];
                        resArray.Initialize();
                        if ((readBlockSize = inputStream.ReadBlock(resArray, 0, readBlockSize)) == 0)
                        {
                            yield break;
                        }
                        var resString = new string(resArray);

                        if (String.IsNullOrEmpty(resString))
                        {
                            yield break;
                        }
                        yield return resString;
                    }
                }
            }
        }

        /// <summary>
        /// Метод используется для получения последовательности слов словаря из указанного входного файла. Возвращает массив слов словаря.
        /// </summary>
        /// <param name="fileName">Имя файла словаря.</param>
        /// <returns>Массив слов словаря.</returns>
        protected string[] GetDictionaryFileContent(string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fileStream.Length > 2097152)
                {
                    throw new ApplicationException("Размер файла словаря превышает максимальный допустимый размер!");
                }
                using (var reader = new StreamReader(fileStream, Encoding.GetEncoding("windows-1251")))
                {
                    var res = new List<string>();
                    while (true)
                    {
                        var curLine = reader.ReadLine();
                        if (curLine == null)
                        {
                            break;
                        }
                        res.Add(curLine);
                    }
                    return res.ToArray();
                }

            }
        }
    };
}
