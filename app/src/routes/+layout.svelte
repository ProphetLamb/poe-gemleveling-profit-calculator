<script lang="ts">
	import RootDrawer from '$lib/client/RootDrawer.svelte';
	import RootFooter from '$lib/client/RootFooter.svelte';
	import '../app.postcss';
	import { AppShell, Modal, type ModalComponent } from '@skeletonlabs/skeleton';
	import { initializeStores } from '@skeletonlabs/skeleton';
	import { computePosition, autoUpdate, flip, shift, offset, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';
	import GemFilterModal from '$lib/client/GemFilterModal.svelte';
	import RootHeader from '$lib/client/RootHeader.svelte';
	import { leagues } from '$lib/client/leagues';
	import { page } from '$app/stores';
	import { showFlash } from '$lib/client/toast';
	import Toast from '$lib/client/Toast.svelte';

	export let data;

	initializeStores();

	$leagues = data.leagues;

	const modals: Record<string, ModalComponent> = {
		gemFilterModal: { ref: GemFilterModal }
	};
	storePopup.set({ computePosition, autoUpdate, flip, shift, offset, arrow });

	showFlash(page);
</script>

<Toast />
<Modal components={modals} />
<RootDrawer />

<div class="contents bg-image-blur h-full overflow-hidden">
	<AppShell>
		<svelte:fragment slot="header">
			<RootHeader />
		</svelte:fragment>
		<svelte:fragment slot="footer">
			<RootFooter />
		</svelte:fragment>

		<!-- Page Route Content -->
		<slot />
	</AppShell>
</div>

<style lang="postcss">
	:global(div.bg-image-blur) {
		position: relative;

		&::before {
			content: '';
			position: absolute;
			top: 0;
			left: 0;
			width: 100%;
			height: 100%;
			background-size: contain;
			background-position: center;
			background-image: url(/background.png);
			background-repeat: no-repeat;
			z-index: -1;
			filter: blur(calc(100vw * 0.003)) brightness(0.95);
			-webkit-filter: blur(calc(100vw * 0.003)) brightness(0.95);
			image-rendering: optimizeSpeed;
		}

		& > div {
		}
	}
</style>
