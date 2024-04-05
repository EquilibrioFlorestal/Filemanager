# Filemanager

## Estrutura

* [Database](./Peixe.Database): Armazena todas as iterações com os bancos de dados associados, conexão, querys.


* [Domain](./Peixe.Domain): Contém todos os modelos de implementação dos recursos utilizados no sistema e pelo módulo Database. 


* [Worker](./Peixe.Worker): Contém o programa principal, um projeto BackgroundWorker + API.

## Serviço

Caso seja necessário, o aplicativo pode ser instalado para ser usado como um serviço do Windows, talvez para esta implantação seja necessário mais alguns ajustes no ambiente do Windows, para que o aplicativo tenha permissões necessárias para manipular os dados no ambiente.

## Objetivo

A função deste aplicativo é realizar o download dos arquivos exportados via Onedrive, separar, verificar cada dado e alocar na pasta correspondente.
A implantação desta aplicação auxilia nas tarefas diárias dos colaboradores, evitando perda de arquivos, e evitando processamento desnecessário dos arquivos.
Ao realizar o download de um arquivo, o mesmo será registrado em uma base de dados, suas informações serão lidas e cadastradas para comporem o IDP.

## Utilização

O aplicativo foi desenvolvido para ser executado em segundo plano no Windows, por conta disto, não é ncessário qualquer interação com o console da aplicação.
Para iniciar o aplicativo, basta executar o arquivo `FileManager.exe`, garanta que na mesma pasta do executável, se encontre os arquivos de `requests.json` e `appsettings.json`, pois são arquivos necesários para a inicialização da aplicação. 

## Manutenção

Para realizar a manutenção da aplicação, realize a leitura dos arquivos `appsettings.json` e `requests.json`.

`appsettings.json`:
Arquivo de configurações do programa, é recomendado apenas alterar a seguinte estrutura 
```json
  "Peixe": {
    "delaySecondsTask": 5, // delay entre cada iteração.
    "delayHoursBackgroundTagOffline": 8, // delay entre cada processamento em segundo plano.
    "maxBatchTask": 2 // máximo de arquivos baixados por cada iteração.    
  },
`````


`requests.json`: Arquivo de configuração das tarefas executadas pelo aplicativo.

```json
[
  {
    "idEmpresa": 1, // id da empresa -> segundo o banco EPF.
    "pastaOrigem": ["", ""], // lista de caminhos a serem utilizados na busca recursiva.
    "pastaDestino": "", // pasta para onde será copiado o dado.
    "pastaBackup": "", // pasta para onde será movido o arquivo.
    "modulo": "DICE" // modulo -> segundo o banco EPF.
  }
]
`````

## Health Check

Ao iniciar este programa, o mesmo irá iniciar um micro-serviço HTTP (port: 50757) com uma única rota /health. Esta rota tem como objetivo apenas retornar o status code 200, indicando que o serviço está em funcionamento. Programas externos podem enviar uma requisição para
`http://ip-machine:50757/health`
e verificar o status code recebido, se retornar 200, indica sucesso.

## Fluxograma

Fluxograma referente à lógica aplicada no processamento dos dados.

![Diagrama de fluxo dos dados](./diagram.png)
