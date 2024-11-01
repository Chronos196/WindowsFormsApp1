using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Database3.accdb;Persist Security Info=False;";
        private OleDbDataAdapter adapter;
        private DataTable currentTable;

        public Form1()
        {
            InitializeComponent();
            //LoadTableNames();
        }

        private void LoadTableNames()
        {
            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    DataTable schemaTable = connection.GetSchema("Tables");

                    foreach (DataRow row in schemaTable.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        string tableType = row["TABLE_TYPE"].ToString();

                        if (!tableName.StartsWith("MSys") && tableType != "SYSTEM TABLE" && tableName!="Users")
                        {
                            comboBox1.Items.Add(tableName);
                        }
                    }
                }

                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке таблиц: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                // Продолжить загрузку данных, если вход успешен
                LoadTableNames();
            }
            else
            {
                // Закрыть приложение, если вход не был успешным
                Application.Exit();
            }
        }

        private void LoadTableData(string tableName)
        {
            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    string query = $"SELECT * FROM [{tableName}]";
                    adapter = new OleDbDataAdapter(query, connection);

                    // Генерируем команды для добавления, обновления и удаления
                    OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(adapter);

                    currentTable = new DataTable();
                    adapter.Fill(currentTable);

                    dataGridView1.DataSource = currentTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных таблицы: {ex.Message}");
            }
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string selectedTable = comboBox1.SelectedItem.ToString();
            LoadTableData(selectedTable);
        }

        private string ExecuteQuery(string query)
        {
            string result = "";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                // Проверяем, является ли поле датой
                                if (reader[i] is DateTime)
                                {
                                    // Форматируем дату без времени
                                    result += ((DateTime)reader[i]).ToString("dd.MM.yyyy") + "\t"; // Формат даты: день.месяц.год
                                }
                                else
                                {
                                    result += reader[i].ToString() + "\t"; // Используем табуляцию для разделения значений
                                }
                            }
                            result += Environment.NewLine; // Переход на новую строку после каждой записи
                        }
                    }
                }
            }

            return result == "" ? "Нет данных." : result.Trim(); // Возвращаем сообщение о том, что нет данных, если результат пустой
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 1. Определить самого молодого предпринимателя в районе 'Киевский'.
            string query = @"
                SELECT TOP 1 ФИО, Дата_рождения
                FROM Владельцы
                WHERE Адрес LIKE '%Киевский%'
                ORDER BY Дата_рождения DESC;";

            string result = ExecuteQuery(query);
            MessageBox.Show(result, "Самый молодой предприниматель");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 2. Определить случаи, когда регистрировалось владение лицами, не достигшими 18 лет.
            string query = @"
                SELECT ФИО, Дата_рождения
                FROM Владельцы
                WHERE YEAR(Дата_рождения) > YEAR(DateAdd('yyyy', -18, Date()));";

            string result = ExecuteQuery(query);
            MessageBox.Show(result, "Владельцы младше 18 лет");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // 3. Определить случаи, когда более 50% уставного капитала магазина внесено предпринимателем из другого района.
            string query = @"
                SELECT 
            M.[Название_магазина],
            M.[Адрес] AS [Адрес_магазина],
            O.[ФИО] AS [ФИО_владельца],
            O.[Адрес] AS [Адрес_владельца],
            SUM(O.[Размер_вклада]) AS [Вклад_другого_района],
            M.[Уставной_капитал]
        FROM 
            [Собственность] AS S,
            [Владельцы] AS O,
            [Магазины] AS M
        WHERE 
            S.[ID_Владельца] = O.[ID_Владельца] AND 
            S.[ID_Магазина] = M.[ID_Магазина] AND
            O.[Адрес] <> M.[Адрес]
        GROUP BY 
            M.[ID_Магазина], 
            M.[Название_магазина], 
            M.[Адрес], 
            O.[ФИО], 
            O.[Адрес], 
            M.[Уставной_капитал]
        HAVING 
            SUM(O.[Размер_вклада]) > 0.5 * M.[Уставной_капитал];";

            string result = ExecuteQuery(query);
            MessageBox.Show(result, "Магазины с более чем 50% уставного капитала");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string query = @"
               SELECT [Магазины].[Профиль], [Магазины].[Уставной_капитал]
                    FROM [Магазины]
                    WHERE [Магазины].[ID_Магазина] IN (
                        SELECT [Собственность].[ID_Магазина]
                        FROM [Собственность]
                        WHERE [Собственность].[ID_Владельца] IN (
                            SELECT [Владельцы].[ID_Владельца]
                            FROM [Владельцы]
                            WHERE [Владельцы].[ФИО] LIKE '%Кузнецов%'
                        )
                    )
                    ORDER BY [Магазины].[Уставной_капитал] DESC;
                ";

            string result = ExecuteQuery(query);
            MessageBox.Show(result, "Профили магазинов Кузнецова");
        }
    }
}