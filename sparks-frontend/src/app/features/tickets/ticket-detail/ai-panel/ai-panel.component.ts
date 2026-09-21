import { Component, Input, OnChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { AiService } from '../../../../core/services/ai.service';
import { AiChatMessage, AiTicketMetadata, SuggestedSolution } from '../../../../core/models/ai.model';

@Component({
  selector: 'sp-ai-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  templateUrl: './ai-panel.component.html',
  styleUrl: './ai-panel.component.scss',
})
export class AiPanelComponent implements OnChanges {
  @Input({ required: true }) ticketId!: string;
  @Input({ required: true }) domain!: string;

  metadata = signal<AiTicketMetadata | null>(null);
  loading = signal(true);
  showFullSummary = signal(false);
  chatDraft = '';
  chatSending = signal(false);

  constructor(private aiService: AiService) {}

  ngOnChanges(): void {
    if (!this.ticketId) return;
    this.loading.set(true);
    this.aiService.getTicketMetadata(this.ticketId, this.domain).subscribe((m) => {
      this.metadata.set(m);
      this.loading.set(false);
    });
  }

  vote(solution: SuggestedSolution, vote: 'up' | 'down'): void {
    const meta = this.metadata();
    if (!meta) return;
    const updated: AiTicketMetadata = {
      ...meta,
      suggestedSolutions: meta.suggestedSolutions.map((s) =>
        s.id === solution.id ? { ...s, vote: s.vote === vote ? null : vote } : s
      ),
    };
    this.metadata.set(updated);
  }

  sendChat(): void {
    const question = this.chatDraft.trim();
    const meta = this.metadata();
    if (!question || !meta) return;
    const userMsg: AiChatMessage = {
      id: `m-${Date.now()}`,
      sender: 'user',
      text: question,
      timestamp: new Date().toISOString(),
    };
    this.metadata.set({ ...meta, chat: [...meta.chat, userMsg] });
    this.chatDraft = '';
    this.chatSending.set(true);
    this.aiService.askAssistant(this.ticketId, question).subscribe((reply) => {
      const current = this.metadata();
      if (!current) return;
      const aiMsg: AiChatMessage = {
        id: `m-${Date.now()}-ai`,
        sender: 'ai',
        text: reply,
        timestamp: new Date().toISOString(),
      };
      this.metadata.set({ ...current, chat: [...current.chat, aiMsg] });
      this.chatSending.set(false);
    });
  }
}
