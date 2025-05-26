using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Classe de compatibilidade - herda de PlayerInventoryManager para manter compatibilidade com código existente
/// </summary>
public class Inventory : PlayerInventoryManager
{
    // Esta classe existe apenas para manter compatibilidade com código existente
    // que referencia "Inventory" em vez de "PlayerInventoryManager"
    
    // Todos os métodos e propriedades são herdados de PlayerInventoryManager
    // Não é necessário reimplementar nada aqui
    
    // Se houver métodos específicos que eram únicos da classe Inventory original,
    // eles podem ser adicionados aqui
}