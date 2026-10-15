Sistema de pedidos — Loja de produtos de limpeza

Mensagens

M1: “Operação realizada com sucesso.”  
M2: “Pedido enviado com sucesso.”  
M3: “Cadastro enviado para análise.”

Exceções

E1: Dados obrigatórios não informados  
Mensagem: “Preencha todos os campos obrigatórios.”

E2: CPF ou CNPJ inválido  
Mensagem: “CPF ou CNPJ inválido.”

E3: Cliente não aprovado  
Mensagem: “Cadastro ainda não aprovado pela empresa.”

E4: Produto indisponível  
Mensagem: “Produto não está disponível para pedidos.”

E5: Falha no envio do pedido  
Mensagem: “Não foi possível enviar o pedido. Verifique sua conexão e tente novamente.”

E6: Falha de conexão com a API  
Mensagem: “Não foi possível conectar ao sistema da empresa.”

Requisitos funcionais

RF-01: Cadastrar cliente

Função: permitir que o cliente informe seus dados para comprar na loja.  
Pré-condição: cliente ainda não possui cadastro no sistema.  
Entrada: tipo_pessoa, nome, CPF, telefone, e-mail, endereço, razão_social e CNPJ.  
Saída: cadastro registrado ou E1/E2.  
Destino: [API / banco de dados da empresa]  
Pós-condição: cadastro fica pendente ou aprovado, conforme a regra da empresa.

RF-02: Consultar cadastro

Função: localizar e exibir o cadastro do cliente.  
Pré-condição: cliente cadastrado.  
Entrada: CPF ou CNPJ.  
Saída: dados do cliente ou cadastro não encontrado.  
Destino: [API / banco de dados da empresa]  
Pós-condição: cadastro exibido para consulta.

RF-03: Consultar produtos

Função: exibir os produtos disponíveis para compra.  
Pré-condição: API disponível.  
Entrada: categoria e nome_produto.  
Saída: lista de produtos com código, descrição, categoria, unidade, tamanho ou volume e preço.  
Destino: [API / banco de dados da empresa]  
Pós-condição: produtos disponíveis para seleção ou E6.

RF-04: Consultar categorias

Função: exibir as categorias de produtos cadastradas pela empresa.  
Pré-condição: API disponível.  
Entrada: nenhuma.  
Saída: lista de categorias.  
Destino: [API / banco de dados da empresa]  
Pós-condição: categorias disponíveis para filtrar produtos.

RF-05: Montar pedido

Função: permitir que o cliente selecione produtos e quantidades.  
Pré-condição: cliente cadastrado e aprovado; produtos disponíveis.  
Entrada: CPF ou CNPJ, lista_produtos e quantidades.  
Saída: resumo do pedido com itens e valor total.  
Destino: [aplicativo / SQLite local]  
Pós-condição: pedido preparado para revisão ou E3/E4.

RF-06: Calcular total do pedido

Função: calcular o valor total dos produtos selecionados.  
Pré-condição: pedido contém pelo menos um produto.  
Entrada: produtos, quantidades e preços.  
Saída: valor_total.  
Destino: [aplicativo]  
Pós-condição: total apresentado antes do envio.

RF-07: Enviar pedido

Função: enviar o pedido do cliente para a empresa.  
Pré-condição: cadastro aprovado e pedido revisado.  
Entrada: id_cliente, data, lista_produtos, quantidades, preços e forma_pagamento.  
Saída: número do pedido e M2, ou E3/E5.  
Destino: [API / banco de dados da empresa]  
Pós-condição: pedido registrado com status “Recebido”.

RF-08: Consultar pedidos anteriores

Função: exibir os pedidos já realizados pelo cliente.  
Pré-condição: cliente cadastrado.  
Entrada: CPF ou CNPJ e período.  
Saída: lista de pedidos com data, valor total e status.  
Destino: [API / banco de dados da empresa]  
Pós-condição: histórico do cliente exibido.

RF-09: Repetir pedido

Função: criar um novo pedido com base em um pedido anterior.  
Pré-condição: cliente possui ao menos um pedido anterior.  
Entrada: id_pedido.  
Saída: novo pedido preenchido com os produtos e quantidades anteriores.  
Destino: [aplicativo / SQLite local]  
Pós-condição: cliente pode revisar e alterar o pedido antes de enviá-lo.

RF-10: Acompanhar pedido

Função: consultar o status de um pedido.  
Pré-condição: pedido registrado.  
Entrada: id_pedido e id_cliente.  
Saída: status atual do pedido.  
Destino: [API / banco de dados da empresa]  
Pós-condição: cliente visualiza o andamento do pedido.

RF-11: Gerenciar produtos

Função: cadastrar, alterar ou desativar produtos no sistema da empresa.  
Pré-condição: funcionário autorizado acessou o sistema do computador.  
Entrada: código, nome, descrição, categoria, unidade, tamanho, preço e status.  
Saída: M1 ou dados inválidos.  
Destino: [sistema da empresa / banco de dados]  
Pós-condição: produto atualizado e disponível na API.

RF-12: Consultar pedidos da loja

Função: permitir que a empresa consulte os pedidos recebidos.  
Pré-condição: funcionário autorizado acessou o sistema do computador.  
Entrada: período, cliente e status.  
Saída: lista de pedidos.  
Destino: [sistema da empresa / banco de dados]  
Pós-condição: pedidos exibidos para análise e atendimento.

RF-13: Atualizar status do pedido

Função: permitir que a empresa atualize o andamento de um pedido.  
Pré-condição: pedido registrado e funcionário autorizado.  
Entrada: id_pedido e status.  
Saída: M1 ou pedido não encontrado.  
Destino: [sistema da empresa / banco de dados]  
Pós-condição: novo status disponível para consulta pelo cliente.

RF-14: Gerenciar clientes

Função: permitir que a empresa consulte, aprove ou atualize cadastros.  
Pré-condição: funcionário autorizado acessou o sistema do computador.  
Entrada: id_cliente, dados_cadastrais e status_cadastro.  
Saída: M1 ou cadastro não encontrado.  
Destino: [sistema da empresa / banco de dados]  
Pós-condição: cadastro atualizado; se aprovado, cliente pode realizar pedidos.
