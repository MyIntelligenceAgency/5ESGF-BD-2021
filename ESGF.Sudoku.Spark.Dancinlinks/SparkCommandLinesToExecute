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
/Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks/bin/Debug/netcoreapp3.1/microsoft-spark-3-0_2.12-1.0.0.jar  \
dotnet /Users/yassine/Documents/GitHub/5ESGF-BD-2021/ESGF.Sudoku.Spark.Dancinlinks/bin/Debug/netcoreapp3.1/ESGF.Sudoku.Spark.Dancinlinks.dll
