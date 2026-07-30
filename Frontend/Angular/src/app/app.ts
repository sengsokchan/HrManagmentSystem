import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HrStateService } from './Core/services/hr-state.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`
})
export class App implements OnInit {
  constructor(private readonly state: HrStateService) {}

  async ngOnInit(): Promise<void> {
    await this.state.init();
  }
}
