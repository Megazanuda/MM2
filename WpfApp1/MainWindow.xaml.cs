using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SolveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Чтение данных из полей
                var objectiveFunction = txtObjectiveFunction.Text;
                var constraints = txtConstraints.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                var c = ParseObjectiveFunction(objectiveFunction);
                var a = ParseConstraints(constraints, out var b);

                // Решение задачи симплексным методом
                var result = Simplex(c, a, b);

                // Вывод решения
                txtSolution.Text = $"Решение: {string.Join(", ", result.Select(r => r.ToString("F2")))}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // Разбор целевой функции
        private double[] ParseObjectiveFunction(string input)
        {
            // Убираем пробелы в начале и в конце строки
            input = input.Replace(" ", "");

            // Разделяем строку на элементы по знакам "+" и "-"
            var terms = new List<string>();
            int startIndex = 0;

            // Проходим по строке, чтобы правильно разделить на термины
            for (int i = 1; i < input.Length; i++)
            {
                if ((input[i] == '+' || input[i] == '-') && (char.IsLetter(input[i - 1]) || char.IsDigit(input[i - 1])))
                {
                    terms.Add(input.Substring(startIndex, i - startIndex));
                    startIndex = i;
                }
            }
            // Добавляем последний термин
            terms.Add(input.Substring(startIndex));

            // Инициализируем список для коэффициентов
            var coefficients = new List<double>();

            foreach (var term in terms)
            {
                // Пытаемся распарсить коэффициент
                if (term.Contains("x") || term.Contains("y"))
                {
                    // Извлекаем коэффициент перед переменной
                    string coefficientString = term.Replace("x", "").Replace("y", "");
                    if (string.IsNullOrEmpty(coefficientString) || coefficientString == "+" || coefficientString == "-")
                    {
                        coefficientString = (coefficientString == "+" || coefficientString == "") ? "1" : "-1";
                    }

                    if (double.TryParse(coefficientString, out double coefficient))
                    {
                        coefficients.Add(coefficient);
                    }
                    else
                    {
                        throw new Exception($"Невозможно разобрать коэффициент в термине '{term}'");
                    }
                }
                else
                {
                    throw new Exception($"Невозможно распознать переменную в термине '{term}'");
                }
            }

            return coefficients.ToArray();
        }

        // Разбор ограничений
        private double[,] ParseConstraints(string[] constraints, out double[] b)
        {
            var a = new List<List<double>>();
            b = new double[constraints.Length];

            foreach (var constraint in constraints)
            {
                // Убираем пробелы и находим позицию символа "<="
                string trimmedConstraint = constraint.Replace(" ", "");
                int lessThanIndex = trimmedConstraint.IndexOf("<=");

                if (lessThanIndex == -1)
                    throw new Exception($"Некорректный формат ограничения: {constraint}");

                // Разделяем строку на левую и правую часть
                string leftPart = trimmedConstraint.Substring(0, lessThanIndex);
                string rightPart = trimmedConstraint.Substring(lessThanIndex + 2);

                // Парсим правую часть (значение ограничения)
                if (!double.TryParse(rightPart, out double rhs))
                {
                    throw new Exception($"Невозможно распарсить правую часть ограничения: {rightPart}");
                }
                b[Array.IndexOf(constraints, constraint)] = rhs;

                // Разбираем левую часть (коэффициенты переменных)
                var coefficients = new List<double>();

                // Для простоты примем, что у нас есть только переменные x и y
                string[] variables = { "x", "y" };

                foreach (var variable in variables)
                {
                    string varPattern = variable;
                    int varIndex = leftPart.IndexOf(varPattern);

                    if (varIndex != -1)
                    {
                        // Если переменная найдена, извлекаем коэффициент перед переменной
                        string coefficient = GetCoefficient(leftPart, varPattern, varIndex);
                        coefficients.Add(double.Parse(coefficient));
                    }
                    else
                    {
                        // Если переменная не найдена, добавляем ноль
                        coefficients.Add(0);
                    }
                }

                a.Add(coefficients);
            }

            // Создаем двумерный массив для коэффициентов
            int m = a.Count; // количество ограничений
            int n = a[0].Count; // количество переменных (x и y)

            double[,] result = new double[m, n];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = a[i][j];
                }
            }

            return result;
        }

        private string GetCoefficient(string leftPart, string variable, int varIndex)
        {
            // Ищем коэффициент перед переменной
            string coefficient = "";

            if (varIndex > 0 && (char.IsDigit(leftPart[varIndex - 1]) || leftPart[varIndex - 1] == '+' || leftPart[varIndex - 1] == '-'))
            {
                int start = varIndex - 1;
                while (start >= 0 && (char.IsDigit(leftPart[start]) || leftPart[start] == '+' || leftPart[start] == '-'))
                {
                    start--;
                }
                coefficient = leftPart.Substring(start + 1, varIndex - start - 1);
            }

            if (string.IsNullOrEmpty(coefficient) || coefficient == "+" || coefficient == "-")
            {
                coefficient = (coefficient == "-") ? "-1" : "1";
            }

            return coefficient;
        }

        // Основной алгоритм симплекс-метода
        private double[] Simplex(double[] c, double[,] a, double[] b)
        {
            int m = a.GetLength(0); // количество ограничений
            int n = a.GetLength(1); // количество переменных

            var tableau = new double[m + 1, n + m + 1];
            var nonZeroArray = new double[m + 1, n + m + 1];

            // Заполнение таблицы симплекс-метода
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    tableau[i, j] = a[i, j];
                }
                tableau[i, n + i] = 1;
                tableau[i, tableau.GetLength(1) - 1] = b[i];
            }

            for (int j = 0; j < n; j++)
            {
                tableau[m, j] = -c[j];
            }

            while (true)
            {
                // Находим самый отрицательный коэффициент в последней строке
                int pivotCol = -1;
                double minValue = 0;
                for (int j = 0; j < tableau.GetLength(1) - 1; j++)
                {
                    if (tableau[m, j] < minValue)
                    {
                        minValue = tableau[m, j];
                        pivotCol = j;
                    }
                }

                if (pivotCol == -1) break; // решение найдено

                // Находим строку для выбора опорного элемента
                int pivotRow = -1;
                double minRatio = double.PositiveInfinity;

                for (int i = 0; i < m; i++)
                {
                    if (tableau[i, pivotCol] > 0)
                    {
                        double ratio = tableau[i, tableau.GetLength(1) - 1] / tableau[i, pivotCol];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1) throw new Exception("Задача не ограничена");

                // Преобразуем таблицу
                PerformPivotOperation(tableau, pivotRow, pivotCol);
                //int nonZeroCount = 0;

                //// Считаем количество ненулевых элементов
                //for (int i = 0; i < tableau.GetLength(0); i++)
                //{
                //    for (int j = 0; j < tableau.GetLength(1); j++)
                //    {
                //        if (tableau[i, j] != 0.0)
                //        {
                //            nonZeroCount++;
                //        }
                //    }
                //}

                //// Создаем новый массив для ненулевых элементов
                
                //int indexi = 0;
                //int indexj = 0;

                //// Заполняем новый массив
                //for (int i = 0; i < tableau.GetLength(0); i++)
                //{
                   
                //    for (int j = 0; j < tableau.GetLength(1); j++)
                //    {
                //        if (tableau[i, j] != 0.0)
                //        {
                //            nonZeroArray[i,indexj] = tableau[i, j];
                //            indexj++;
                //        }
                //    }
                //    indexi++;
                //}
            }


            // Выводим результат
            double[] result = new double[m];

            for (int i = 0; i < m; i++)
            {
                //if (tableau[i, n + m] == 1)
                //{
                    result[i] = tableau[i, tableau.GetLength(1) - 1];
                //}
            }

            return result;
        }

        private void PerformPivotOperation(double[,] tableau, int pivotRow, int pivotCol)
        {
            double pivotValue = tableau[pivotRow, pivotCol];
            for (int j = 0; j < tableau.GetLength(1); j++)
            {
                tableau[pivotRow, j] /= pivotValue;
            }

            for (int i = 0; i < tableau.GetLength(0); i++)
            {
                if (i != pivotRow)
                {
                    double factor = tableau[i, pivotCol];
                    for (int j = 0; j < tableau.GetLength(1); j++)
                    {
                        tableau[i, j] -= factor * tableau[pivotRow, j];
                    }
                }
            }
        }
    }
}
