# Projet C#

## Résolution de sudokus avec Dancinlinks

***** Le code est commenté dans le Program.cs***** 

### 1- Introduction

Nous nous sommes inspiré du repo https://github.com/taylorjg/SudokuDlx pour intégrer le code de résolution de sudoku avec la méthode des DancinLinks

### 2- Modifications apportées au code original

#### 2.1 Adaptation pour prise en compte d'un fichier csv

Après avoir défini une variable _filePath (qui se trouve au dessus du main()) poitant vers le fichier csv contenant les sudokus (qui se trouve dans le dossier du projet console), il a fallut modifier comment était appelé le constructeur de la classe Grid. Les sudokus dans le fichier csv étaient sous la forme CONCAT(ligne1, ligne2 ...), il a été nécéssaire de faire appel à la fonction Substring() pour créer l'ImmutableList nécéssaire pour le constructeur.

```c#
            var grid = new Grid(ImmutableList.Create(
            sudoku.Substring(0, 9),
            sudoku.Substring(9, 9),
            sudoku.Substring(18, 9),
            sudoku.Substring(27, 9),
            sudoku.Substring(36, 9),
            sudoku.Substring(45, 9),
            sudoku.Substring(54, 9),
            sudoku.Substring(63, 9),
            sudoku.Substring(72, 9)));
```
au lieu de :

```c#
        var grid = new Grid(ImmutableList.Create(
            "6 4 9 7 3",
            "  3    6 ",
            "       18",
            "   18   9",
            "     43  ",
            "7   39   ",
            " 7       ",
            " 4    8  ",
            "9 8 6 4 5"));
```
#### 2.2 Intégration de Spark

##### 2.2.1 Création de deux méthodes

Deux nouvelles méthodes ont été créées pour sortir le code de résolution de sudoku du main().

Sudokusolution(string sudoku){} qui elle contient le code nécéssaire pour résoudre le sudoku en entrée qui est sous forme de string.

```c#
    //Création d'une méthode qui prend en entrée un string (le sudoku non résolu) et qui renvoie un string (le sudoku résolu).
    //De base le code duquel on s'est inspiré ne renvoyait rien, la sortie était sous forme d'un Console.WriteLine()
    private static string Sudokusolution(string sudoku){

            //Récupération du sudoku à résoudre depuis le string en entrée et transfert dans une ImmutableList.
            var grid = new Grid(ImmutableList.Create(
            sudoku.Substring(0, 9),
            sudoku.Substring(9, 9),
            sudoku.Substring(18, 9),
            sudoku.Substring(27, 9),
            sudoku.Substring(36, 9),
            sudoku.Substring(45, 9),
            sudoku.Substring(54, 9),
            sudoku.Substring(63, 9),
            sudoku.Substring(72, 9)));

            var internalRows = BuildInternalRowsForGrid(grid);
            var dlxRows = BuildDlxRows(internalRows);

            var solutions = new Dlx()
                .Solve(BuildDlxRows(internalRows), d => d, r => r)
                .Where(solution => VerifySolution(internalRows, solution))
                .ToImmutableList();

            if (solutions.Any()){
                //Enlever commentaire pour afficher les solutions dans la console

                //Console.WriteLine($"First solution (of {solutions.Count}):");
                //Console.WriteLine();
                //SolutionToGrid(internalRows, solutions.First()).Draw();
                //Console.WriteLine();

                //Ajout de ce bout de code pour avoir une sortie de type string contenant le sudoku résolu.
                string s = "";
                for (int i = 0; i <= 8; i++){
                    for (int j = 0; j <= 8; j++){
                        s += SolutionToGrid(internalRows, solutions.First()).ValueAt(i, j).ToString();
                    }
                }

                return s;
            } else {
                //Console.WriteLine("No solutions found!");
                return "No solutions found!";
            }
    
    }
```
Le code original renvoyant le sudoku résolu sous forme de Console.WriteLine(), il a été nécéssaire de l'adapter avec deux boucles for, pour obtenir un string en sortie grace à la fonction .ValueAt qui était fournie dans le code original. Il faut dé-commenter les Console.WriteLine pour avoir les sudokus résolus en format "Grille" dans la console.

```c#
if (solutions.Any()){
                //Enlever commentaire pour afficher les solutions dans la console	               
								//Console.WriteLine($"First solution (of {solutions.Count}):");
                //Console.WriteLine();
                //SolutionToGrid(internalRows, solutions.First()).Draw();
                //Console.WriteLine();

                //Ajout de ce bout de code pour avoir une sortie de type string contenant le sudoku résolu.
                string s = "";
                for (int i = 0; i <= 8; i++){
                    for (int j = 0; j <= 8; j++){
                        s += SolutionToGrid(internalRows, solutions.First()).ValueAt(i, j).ToString();
                    }
                }

                return s;
            } else {
                //Console.WriteLine("No solutions found!");
                return "No solutions found!";
            }
```
La deuxième méthode créée, Sudokures(string cores, string nodes, int nrows){} quant à elle contient le code pour intialiser la SparkSession (avec les paramètres pour le nombre de cores et le nombre d'instances, la création du DataFrame et le transfert des données du csv dessus, la limitation du nombre de ligne/sudoku a traiter (nrows).

En plus de cela elle contient aussi la création de l'UDF Spark et l'appel de cette UDF au travers d'une requête SQL.

Ces dernières actions quant à elles sont entourées d'une variable Stopwatch qui va nous permettre de mesurer le temps requis pour traiter nrows sudokus.

```c#
//Méthode qui est appelée depuis le main pour lancer une session spark avec un nombbre de noyaux et d'instances différents et lancer la résolution du soduku grace à la méthode Sudokusolution().
        private static void Sudokures(string cores, string nodes, int nrows){
            // Initialisation de la session Spark
            SparkSession spark = SparkSession
                .Builder()
                .AppName("Resolution of "+ nrows + " sudokus using DlxLib with " + cores + " cores and " + nodes + " instances")
                .Config("spark.executor.cores", cores)
                .Config("spark.executor.instances", nodes)
                .GetOrCreate();

				// Intégration du csv dans un dataframe
        DataFrame df = spark
            .Read()
            .Option("header", true)
            .Option("inferSchema", true)
            .Csv(_filePath);

        //limit du dataframe avec un nombre de ligne prédéfini lors de l'appel de la fonction
        DataFrame df2 = df.Limit(nrows);

        //Watch seulement pour la résolution des sudokus
        var watch2 = new System.Diagnostics.Stopwatch();
        watch2.Start();

        // Création de la spark User Defined Function
        spark.Udf().Register<string, string>(
            "SukoduUDF",
            (sudoku) => Sudokusolution(sudoku));

        // Appel de l'UDF dans un nouveau dataframe spark qui contiendra les résultats aussi
        df2.CreateOrReplaceTempView("Resolved");
        DataFrame sqlDf = spark.Sql("SELECT Sudokus, SukoduUDF(Sudokus) as Resolution from Resolved");
        sqlDf.Show();

        watch2.Stop();

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Execution Time for " + nrows + " sudoku resolution with " + cores + " core and " + nodes + " instance: " + watch2.ElapsedMilliseconds + " ms");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

        spark.Stop();

    }
```
#### 2.3 Initiationsation de la SparkSession depuis le main(), avec paramètres sur le nombre de sudokus, de cores et d'instances.

La méthode Sudokures (qui elle même appelle Sudokusolution) est appelée depuis le main() deux fois, avec des paramètres différents. Deux Stopwatch ont aussi été créés pour mesurer le temps complet d'éxécution (création de la SparkSession, du DataFrame, de l'UDF, résolution des sudokus) et pouvoir benchmarker.

```c#
    public static void Main(){
        //temps d'execution global (chargement du CSV + création DF et sparksession)
        var watch = new System.Diagnostics.Stopwatch();
        var watch1 = new System.Diagnostics.Stopwatch();

        watch.Start();

        //Appel de la méthode, spark session avec 1 noyau et 1 instance, 1000 sudokus à résoudre
        Sudokures("1", "1", 1000);

        watch.Stop();
        watch1.Start();

        //Appel de la méthode, spark session avec 1 noyau et 4 instance, 1000 sudokus à résoudre
        Sudokures("1", "4", 1000);

        watch1.Stop();

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Global Execution (CSV + DF + SparkSession) Time with 1 core and 1 instance: {watch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Global Execution (CSV + DF + SparkSession) Time with 1 core and 4 instances: {watch1.ElapsedMilliseconds} ms");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

    }
```
### 3- Résultats obtenus

Deux tests ont été effectués, un avec 300 sudokus à résoudre, l'autre avec 1000.

Les résultats obtenus sont les suivants :

###### 300 sudokus

Execution Time for 300 sudoku resolution with 1 core and 1 instance: 26074 ms

Execution Time for 300 sudoku resolution with 1 core and 4 instance: 24192 ms

Global Execution (CSV + DF + SparSession) Time with 1 core and 1 instance: 35645 ms

Global Execution (CSV + DF + SparSession) Time with 1 core and 4 instances: 23990 ms

###### 1000 sudokus

Execution Time for 1000 sudoku resolution with 1 core and 1 instance: 71873 ms

Execution Time for 1000 sudoku resolution with 1 core and 4 instance: 66139 ms

Global Execution (CSV + DF + SparkSession) Time with 1 core and 1 instance: 81111 ms

Global Execution (CSV + DF + SparkSession) Time with 1 core and 4 instances: 68294 ms



###### Conclusion

DacinLinks est déjà très rapide et bien optimisé pour résoudre les sudokus avec une différence entre (1 core et 1 instance) et (1 core et 4 instances) peu notable sur 300 sudokus, qui est a peine de l'ordre de 2s.

La différence sur Global Execution Time quant à elle est de 12 secondes, on peut en conclure que la machine a plus rapidement lu et importé le fichier CSV.

Pour 1000 sudokus, la différence entre (1 core et 1 instance) et (1 core et 4 instances) passe à 5 secondes pour la résolution seulement, et sur le Global Execution Time elle est d'à peu près 12 secondes encore comme pour 300 sudokus.

On en conclu que plus le nombre de sudoku à résoudre sera grand, plus la différence entre (1 core et 1 instance) et (1 core et 4 instances) sera notable. Malheureusement sur nos machines, résoudre 1 000 000 de sudoku aurait été trop long mais il serait intérésser de benchmarker ça sur un cluster/serveur plus puissant avec au moins 64 Go de RAM et un processeur plus puissant que nos Intel Core i5.

### 4 - Code d'exécution dans le terminal (macOS) pour lancer le projet avec Spark-Submit

#### À adapter avec les chemins correspondants aux fichiers dans la machine où le code va être exécuté EN PLUS du chemin pour le fichier csv (variable définie avant le main() dans Program.cs)

    export SPARK_HOME=/Users/yassine/Downloads/spark-3.0.1-bin-hadoop2.7
    
    export PATH="$SPARK_HOME/bin:$PATH"
    export DOTNET_WORKER_DIR=/Users/yassine/Downloads/Microsoft.Spark.Worker-1.0.0
    
    cd /Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks
    
    dotnet add package Microsoft.Spark
    
    dotnet build
    
    export DOTNET_ASSEMBLY_SEARCH_PATHS=/Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks/bin/Debug/netcoreapp3.1
    
    spark-submit \
    --class org.apache.spark.deploy.dotnet.DotnetRunner \
    --master local \
    /Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks/bin/Debug/netcoreapp3.1/microsoft-spark-3-0_2.12-1.0.0.jar \
    dotnet /Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks/bin/Debug/netcoreapp3.1/ESGF.Sudoku.Spark.Dancinlinks.dll
