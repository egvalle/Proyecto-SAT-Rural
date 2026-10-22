import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ComponentCardComponent } from '../../../../../shared/components/component-card/component-card.component';


@Component({
  selector: 'app-ejecutivo',
  standalone: true,
  imports: [FormsModule, ComponentCardComponent],
  templateUrl: './ejecutivo.html',
  styleUrl: './ejecutivo.scss',
})
export class Ejecutivo {
  private readonly router = inject(Router);

}