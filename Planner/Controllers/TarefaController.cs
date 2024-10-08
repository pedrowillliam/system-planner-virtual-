﻿using Microsoft.AspNetCore.Mvc;
using Planner.Models.Enum;
using Planner.Models;
using Planner.Service;
using Microsoft.EntityFrameworkCore;

namespace Planner.Controllers
{
    public class TarefaController : Controller
    {
        private readonly TarefaService _tarefaService;

        public TarefaController(TarefaService tarefaService)
        {
            _tarefaService = tarefaService;
        }

        // GET: /Tarefa
        public async Task<IActionResult> Index([FromQuery] Categoria? categoria = null, [FromQuery] StatusTarefa? status = null, [FromQuery] int? mes = null, [FromQuery] int? ano = null, [FromQuery] DateTime? dataInicio = null,
    [FromQuery] DateTime? dataFim = null)
        {
            IEnumerable<Tarefa> tarefas;

            if (categoria.HasValue && status.HasValue)
            {
                tarefas = await _tarefaService.GetTarefasByCategoriaAndStatusAsync(status.Value, categoria.Value);
            }
            else if (categoria.HasValue)
            {
                tarefas = await _tarefaService.GetTarefasByCategoriaAsync(categoria.Value);
            }
            else if (status.HasValue)
            {
                tarefas = await _tarefaService.GetTarefasByStatusAsync(status.Value);
            }
            else
            {
                tarefas = await _tarefaService.GetAllTarefasAsync();
            }

            // Se nenhuma data for fornecida, mostra apenas as tarefas de hoje
            if (!dataInicio.HasValue && !dataFim.HasValue)
            {
                var hoje = DateTime.Now.Date;
                tarefas = tarefas.Where(t => t.Dia.Date == hoje);
            }
            else
            {
                // Filtra tarefas entre as datas fornecidas
                if (dataInicio.HasValue)
                {
                    tarefas = tarefas.Where(t => t.Dia >= dataInicio.Value);
                }

                if (dataFim.HasValue)
                {
                    tarefas = tarefas.Where(t => t.Dia <= dataFim.Value);
                }
            }

            // Filtra por mês e ano, se fornecidos
            if (mes.HasValue)
            {
                tarefas = tarefas.Where(t => t.Dia.Month == mes.Value);
            }

            if (ano.HasValue)
            {
                tarefas = tarefas.Where(t => t.Dia.Year == ano.Value);
            }

            // Ordena as tarefas por data
            tarefas = tarefas.OrderBy(t => t.Dia);

            return View(tarefas); // Renderiza a view 'Index' com a lista de tarefas
        }

        // GET: /Tarefa/Detalhes/5
        public async Task<IActionResult> Detalhes(int id)
        {
            var tarefa = await _tarefaService.GetTarefaByIdAsync(id);

            if (tarefa == null)
            {
                return NotFound();
            }

            return View(tarefa); // Renderiza a view 'Detalhes' com a tarefa específica
        }

        // GET: /Tarefa/Adicionar
        public IActionResult Adicionar()
        {
            return View(); // Renderiza a view 'Adicionar'
        }

        // POST: /Tarefa/Adicionar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adicionar(Tarefa tarefa, BlocoDuracao blocoDuracao)
        {
            if (ModelState.IsValid)
            {

                tarefa.Fim = CalcularFim(tarefa.Inicio, blocoDuracao);

                // Verificar os horários
                try
                {
                    tarefa.Horario(tarefa.Inicio, tarefa.Fim);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("Horario", ex.Message);
                    return View(tarefa);
                }
                
                if (tarefa.Dia == default(DateTime))
                {
                    tarefa.Dia = DateTime.Now.Date;
                }
                tarefa.StatusTarefa = StatusTarefa.NaoIniciada;

                await _tarefaService.AddTarefaAsync(tarefa);
                return RedirectToAction(nameof(Index)); // Redireciona para a ação Index após adicionar a tarefa
            }

            return View(tarefa); // Se o modelo não for válido, retorna à view 'Adicionar' com os dados preenchidos
        }

        // GET: /Tarefa/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var tarefa = await _tarefaService.GetTarefaByIdAsync(id);

            if (tarefa == null)
            {
                return NotFound();
            }

            return View(tarefa); // Renderiza a view 'Editar' com a tarefa específica
        }

        // POST: /Tarefa/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Tarefa tarefaAtualizada, BlocoDuracao blocoDuracao)
        {
            if (id != tarefaAtualizada.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                tarefaAtualizada.Fim = CalcularFim(tarefaAtualizada.Inicio, blocoDuracao);
                try
                {
                    tarefaAtualizada.Horario(tarefaAtualizada.Inicio, tarefaAtualizada.Fim); // Verifica os horários
                    await _tarefaService.UpdateTarefaAsync(tarefaAtualizada);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var existingTarefa = await _tarefaService.GetTarefaByIdAsync(id);
                    if (existingTarefa == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (ArgumentException ex) // Trata exceção de horário inválido
                {
                    ModelState.AddModelError("Horario", ex.Message);
                    return View(tarefaAtualizada);
                }

                return RedirectToAction(nameof(Index));
            }

            return View(tarefaAtualizada);
        }

        // GET: /Tarefa/Deletar/5
        public async Task<IActionResult> Deletar(int id)
        {
            var tarefa = await _tarefaService.GetTarefaByIdAsync(id);

            if (tarefa == null)
            {
                return NotFound();
            }

            return View(tarefa); // Renderiza a view 'Deletar' com a tarefa específica
        }

        // POST: /Tarefa/Deletar/5
        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarConfirmado(int id)
        {
            await _tarefaService.DeleteTarefaAsync(id);
            return RedirectToAction(nameof(Index)); // Redireciona para a ação Index após deletar a tarefa
        }

        // POST: /Tarefa/DeletarTodos
        [HttpPost]
        [Route("DeletarTodos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarTodos()
        {
            await _tarefaService.DeleteAllTarefasAsync();
            return RedirectToAction(nameof(Index)); // Redireciona para a ação Index após deletar todas as tarefas
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarStatus(int id, StatusTarefa novoStatus)
        {
            var tarefa = await _tarefaService.GetTarefaByIdAsync(id);

            if (tarefa != null)
            {
                tarefa.StatusTarefa = novoStatus; // Atualiza o status
                await _tarefaService.UpdateTarefaAsync(tarefa);
                TempData["MensagemSucesso"] = "Status da tarefa atualizado com sucesso!";
            }
            else
            {
                TempData["MensagemErro"] = "Tarefa não encontrada.";
            }

            return RedirectToAction(nameof(Index));
        }

        private TimeSpan CalcularFim(TimeSpan inicio, BlocoDuracao blocoDuracao)
        {
            return blocoDuracao switch
            {
                BlocoDuracao.MeiaHora => inicio.Add(TimeSpan.FromMinutes(30)),
                BlocoDuracao.UmaHora => inicio.Add(TimeSpan.FromHours(1)),
                BlocoDuracao.Manha => new TimeSpan(12, 0, 0),
                BlocoDuracao.Tarde => new TimeSpan(18, 0, 0),
                BlocoDuracao.Noite => new TimeSpan(23, 59, 59),
                BlocoDuracao.DiaTodo => new TimeSpan(23, 59, 59),
                _ => inicio
            };
        }


    }
}