# Criar caminhos e pontos de construção

## Atualizar

1. No GitHub Desktop, clique em **Fetch origin**.
2. Clique em **Pull origin**.
3. Volte para a Unity e aguarde a compilação.

## Gerar

Abra:

```text
Tools → Cats vs Demons → Add Serpentine Paths
```

O comando cria:

- caminho esquerdo longo;
- caminho direito longo;
- caminho inferior longo;
- várias curvas e mudanças de direção;
- nenhuma entrada superior;
- nenhuma rota curta diretamente abaixo da casa;
- três pontos de nascimento dos demônios;
- doze pontos vazios de construção.

O comando preserva terreno, casa, câmera e iluminação. Se for executado novamente, substitui somente os caminhos e pontos de construção.

## Salvar

1. Pressione `Ctrl + S`.
2. No GitHub Desktop, faça o commit:

```text
feat: add serpentine paths and build spots
```

3. Clique em **Push origin**.
