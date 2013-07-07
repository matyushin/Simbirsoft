using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TextHandlerInterface;
using System.IO;
using System.Text.RegularExpressions;

namespace TextHandlerLibrary
{
    /// <summary>
    /// Класс обработчика, формирующего из исходного текста в соответствии со словарем последовательности результирующих тектовых файлов, в которых слова, содержащиеся в словаре, написаны прописью.
    /// </summary>
    public class TextHandler : HandlerLibrary
    {
        /// <summary>
        /// Метод-процесс обработки текущего блока символов.
        /// </summary>
        /// <param name="dict">Словарь.</param>
        /// <param name="curLine">Текущий блок символов.</param>
        /// <param name="outputStream">Открытый результирующий файл (поток), в который записываются результаты обработки.</param>
        /// <param name="curLinesCount">Счетчик записанных в результирующий файл (поток) строк (не больше maxLinesCount).</param>
        /// <returns>Часть переданного блока символов, не прошедший обработку, в случае, если счетчик записанных в результирующий файл (поток) строк превысит maxLinesCount.</returns>
        private static string ProcessorInputTextBlock(ICollection<string> dict, string curLine, StreamWriter outputStream, ref int curLinesCount)
        {
            const string end = "?!.";
            const string wordPattern = @"\b\S*\b";

            var matchWordsInLine = Regex.Matches(curLine, wordPattern, RegexOptions.IgnoreCase);
            var matchCounter = 0;

            for (var i = 0; i < curLine.Length; i++)
            {
                if (!char.IsLetter(curLine[i]))
                {
                    if (PrintIfNotWord(curLine, outputStream, ref curLinesCount, i, end))
                    {
                        return curLine.Substring(i + 1);
                    }
                    continue;
                }

                if (PrintIfWord(matchWordsInLine, matchCounter, ref i, outputStream, dict))
                {
                    matchCounter++;
                }
            }
            return "";
        }

        /// <summary>
        /// Записывает символ с указанной позицией в выходной поток. При нахождении конца строки увеличивает счетчик строк в выходном потоке, и указывает, требуется ли создание еще одного результирующего файла.
        /// </summary>
        /// <param name="curLine">Текущий блок символов, переданный на обработку.</param>
        /// <param name="outputStream">Выходной поток.</param>
        /// <param name="curLinesCount">Счетчик строк в выходном потоке.</param>
        /// <param name="charPos">Позиция символа во входном блоке.</param>
        /// <param name="end">Символы завершения предложения.</param>
        /// <returns>Флаг превышения счетчиком строк в выходном файле максимально допустимого значения.</returns>
        private static bool PrintIfNotWord(string curLine, StreamWriter outputStream, ref int curLinesCount, int charPos, string end)
        {
            outputStream.Write(curLine[charPos]);
            if (curLine[charPos] == '\n')
            {
                curLinesCount++;
            }

            return (curLinesCount >= MaxLinesCount - 1) && (end.Contains(curLine[charPos]));
        }

        /// <summary>
        /// Определяет, является ли символ с указанной позицией частью слова, если да - записывает это слово в выходной поток, предварительно выполняя над ним действия по обработке.
        /// </summary>
        /// <param name="matchWordsInLine">Набор слов во входном блоке.</param>
        /// <param name="matchCounter">Номер слова в наборе слов, с которого начинается поиск.</param>
        /// <param name="charPos">Позиция символа во входном блоке</param>
        /// <param name="outputStream">Выходной поток.</param>
        /// <param name="dict">Словарь.</param>
        private static bool PrintIfWord(MatchCollection matchWordsInLine, int matchCounter, ref int charPos, StreamWriter outputStream, ICollection<string> dict)
        {
            for (var j = matchCounter; j < matchWordsInLine.Count; j++)
            {
                var m = matchWordsInLine[j];
                if ((charPos >= m.Index) && (charPos < m.Index + m.Length))
                {
                    outputStream.Write(!dict.Contains(m.Value.ToLower()) ? m.Value : (m.Value.ToUpper()));
                    charPos += m.Length - 1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Создает результирующий файл (поток) и передает его для записи результатов обработки входного текста. Контролирует количество выходных файлов, при необходимости создает новые.
        /// </summary>
        /// <param name="dict">Словарь слов.</param>
        /// <param name="resultFileName">Имя (путь) результирующего файла (неизменная часть).</param>
        /// <param name="en">Перечислитель, по которому можно получить каждый следующий блок символов из входного потока (файла).</param>
        /// <param name="fileExtension">Расширение результирующего файла.</param>
        protected static void WriteResult(ICollection<string> dict, string resultFileName, IEnumerator<string> en, string fileExtension)
        {
            var flag = true;
            var resFilesCount = 1;
            var outputFileName = resultFileName + resFilesCount.ToString() + fileExtension;

            var remainderLine = "";
            while (flag)
            {
                using (var fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    flag = WorkWithNextFile(fileStream, en, resultFileName, dict, ref remainderLine);

                    if (flag)
                    {
                        resFilesCount++;
                        outputFileName = resultFileName + resFilesCount.ToString() + fileExtension;
                    }
                    fileStream.Close();
                }
            }
        }

        /// <summary>
        /// Обеспечивает вывод обработанного текста в переданный файловый поток.
        /// </summary>
        /// <param name="fileStream">Выходной файловый поток.</param>
        /// <param name="en">Перечислитель, по которому можно получить каждый следующий блок символов из входного потока (файла).</param>
        /// <param name="resultFileName">Имя результирующего файла.</param>
        /// <param name="dict">Словарь.</param>
        /// <param name="remainderLine">Часть блока, остающаяся необработанной (если есть).</param>
        /// <returns>Флаг необходимости создания еще одного выходного файла.</returns>
        private static bool WorkWithNextFile(FileStream fileStream, IEnumerator<string> en, string resultFileName, ICollection<string> dict, ref string remainderLine)
        {
            using (var outputStream = new StreamWriter(fileStream, Encoding.GetEncoding("windows-1251")))
            {
                var curLinesCount = 0;

                while (en.MoveNext())
                {
                    if (en.Current == null)
                    {
                        outputStream.Close();
                        File.Delete(resultFileName);
                        throw new ApplicationException("Текст для обработки недоступен! Обработка прервана!");
                    }
                    remainderLine = ProcessorInputTextBlock(dict, String.Concat(remainderLine, en.Current), outputStream, ref curLinesCount);
                    if (curLinesCount >= MaxLinesCount)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Формирует словарь и возвращает его вызвавшему методу.
        /// </summary>
        /// <param name="dictFileName">Имя (путь) файла со словарем.</param>
        /// <returns>Словарь.</returns>
        private HashSet<string> GetDictionary(string dictFileName)
        {
            var dictionary = new HashSet<string>();
            var dictionaryContent = GetDictionaryFileContent(dictFileName);
            if (dictionaryContent == null)
            {
                throw new Exception("Словарь пуст! Обработка прервана!");
            }

            foreach (var word in dictionaryContent)
            {
                dictionary.Add(word.ToLower());
            }
            return dictionary;
        }

        /// <summary>
        /// Перегруженный метод базового класса HandlerLibrary.
        /// Запускает обработку исходного текста.
        /// </summary>
        public override void Run(string dictFileName, string textFileName, string resultFileName)
        {
            const string inFileExtension = ".txt";
            const string outFileExtension = ".txt";

            var dictionary = GetDictionary(dictFileName + inFileExtension);

            var textContent = GetInputTextFileContent(textFileName + inFileExtension);
            var en = textContent.GetEnumerator();

            WriteResult(dictionary, resultFileName, en, outFileExtension);
        }
    }
}