import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import {
  LEGAL_DOCUMENTS,
  LegalDocumentContent,
  LegalPageId,
} from './legal-document-content';

@Component({
  selector: 'app-legal-document',
  imports: [CommonModule, RouterModule],
  templateUrl: './legal-document.component.html',
  styleUrl: './legal-document.component.scss',
})
export class LegalDocumentComponent implements OnInit {
  content: LegalDocumentContent | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly title: Title,
    private readonly meta: Meta
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.data['legalPageId'] as LegalPageId;
    const doc = id ? LEGAL_DOCUMENTS[id] : undefined;
    if (!doc) {
      return;
    }
    this.content = doc;
    this.title.setTitle(doc.pageTitle);
    this.meta.updateTag({ name: 'description', content: doc.metaDescription });
  }

  hasHeading(section: { heading: string }): boolean {
    return section.heading.trim().length > 0;
  }
}
